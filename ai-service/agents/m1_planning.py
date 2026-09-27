from datetime import datetime, timedelta
import json
from state.workflow_state import WorkflowState
from schemas.models import AgentExecutionSummary, TripPlan
from agents.llm_client import get_gemini_client
from tools.client import InternalAgentClient

M1_SYSTEM_PROMPT = """You are Member 1 (M1), the Trip Planning & Coordination agent for an Intelligent Travel Planning system for Sri Lanka.
Your job is to generate an area-level trip plan given the user's selected destinations and travel dates.

CRITICAL RULES:
1. EndDate cannot precede StartDate.
2. Daily entries cover every trip date, including the final travel day, in order.
3. Each day has a meaningful area-level plan.
4. Every selected area appears at least once in daily_visits.
5. Do NOT add unselected destination areas to the planned visits or overnight selections.
6. Multiple selected areas may be visited on one day.
7. A visited area does not need its own overnight stay.
8. For an overnight trip:
   - Overnight sections cover every required night.
   - First check-in equals StartDate.
   - Final checkout equals EndDate.
   - Each stay has positive duration.
   - Consecutive sections have no gaps or overlaps.
   - Night counts match the dates.
   - Daily overnight areas agree with the overnight sections.
   - Merge adjacent nights in the same area into one section.
   - Separate repeated overnight sections in the same area are DISALLOWED.
9. For a one-day trip (StartDate == EndDate):
   - Produce one day's area visits.
   - Overnight sections MUST be empty.
10. Do not invent attractions, hotel names, room IDs, prices, weather forecasts, or vehicle selections.
11. Keep explanations brief.
"""

def parse_date(date_str: str) -> datetime.date:
    return datetime.fromisoformat(date_str.replace('Z', '+00:00')).date()

def validate_plan(plan: TripPlan, start_date: datetime.date, end_date: datetime.date, selected_destinations: list) -> list[str]:
    errors = []
    
    if end_date < start_date:
        errors.append("EndDate precedes StartDate.")
        return errors
        
    total_days = (end_date - start_date).days + 1
    
    # 1. Daily entries cover every trip date, in order
    if len(plan.daily_visits) != total_days:
        errors.append(f"Expected {total_days} daily visits, got {len(plan.daily_visits)}.")
    
    selected_area_ids = {d['DestinationId'] for d in selected_destinations}
    visited_area_ids = set()
    
    for i, visit in enumerate(plan.daily_visits):
        expected_date = start_date + timedelta(days=i)
        if visit.date != expected_date.isoformat():
            errors.append(f"Day {i+1} date {visit.date} does not match expected {expected_date.isoformat()}")
        if visit.day_number != i + 1:
            errors.append(f"Day {i+1} has incorrect day_number {visit.day_number}")
        
        visited_area_ids.update(visit.visited_area_ids)
        
        # Do not add unselected destination areas
        unselected = set(visit.visited_area_ids) - selected_area_ids
        if unselected:
            errors.append(f"Day {visit.day_number} visits unselected areas: {unselected}")

    # Every selected area appears at least once
    missed_areas = selected_area_ids - visited_area_ids
    if missed_areas:
        errors.append(f"Plan misses selected areas: {missed_areas}")
        
    is_one_day = start_date == end_date
    if is_one_day:
        if plan.overnight_sections:
            errors.append("One-day trip must have empty overnight_sections.")
    else:
        # Overnight trip validation
        expected_nights = total_days - 1
        actual_nights = sum(sec.night_count for sec in plan.overnight_sections)
        if actual_nights != expected_nights:
            errors.append(f"Expected {expected_nights} total nights, got {actual_nights}.")
            
        if not plan.overnight_sections:
            errors.append("Overnight trip must have overnight sections.")
        else:
            first_check_in = parse_date(plan.overnight_sections[0].check_in_date)
            last_check_out = parse_date(plan.overnight_sections[-1].check_out_date)
            
            if first_check_in != start_date:
                errors.append(f"First check-in {first_check_in} must match StartDate {start_date}")
            if last_check_out != end_date:
                errors.append(f"Last check-out {last_check_out} must match EndDate {end_date}")
            
            # Check for gaps/overlaps, positive duration, and repeated sections
            prev_checkout = None
            prev_area_id = None
            for i, sec in enumerate(plan.overnight_sections):
                ci = parse_date(sec.check_in_date)
                co = parse_date(sec.check_out_date)
                
                if (co - ci).days != sec.night_count:
                    errors.append(f"Section {i} night count mismatch.")
                if sec.night_count <= 0:
                    errors.append(f"Section {i} has non-positive duration.")
                    
                if prev_checkout and ci != prev_checkout:
                    errors.append(f"Gap or overlap between section {i-1} checkout {prev_checkout} and section {i} checkin {ci}")
                    
                if prev_area_id == sec.overnight_area_id:
                    errors.append(f"Adjacent nights in same area {sec.overnight_area_id} must be merged.")
                elif sec.overnight_area_id in [s.overnight_area_id for s in plan.overnight_sections[:i]]:
                    errors.append(f"Separate repeated overnight section in area {sec.overnight_area_id} is disallowed.")
                    
                unselected = {sec.overnight_area_id} - selected_area_ids
                if unselected:
                    errors.append(f"Overnight in unselected area: {unselected}")
                    
                prev_checkout = co
                prev_area_id = sec.overnight_area_id

            # Daily overnight areas agree with the overnight sections
            for visit in plan.daily_visits:
                v_date = parse_date(visit.date)
                # Find the overnight section that covers this date
                # if date == end_date, it's the last day, no overnight
                if v_date == end_date:
                    if visit.overnight_area_id is not None:
                        errors.append(f"Last day {visit.date} should not have an overnight area.")
                else:
                    found_section = None
                    for sec in plan.overnight_sections:
                        ci = parse_date(sec.check_in_date)
                        co = parse_date(sec.check_out_date)
                        if ci <= v_date < co:
                            found_section = sec
                            break
                    if found_section:
                        if visit.overnight_area_id != found_section.overnight_area_id:
                            errors.append(f"Day {visit.date} overnight area {visit.overnight_area_id} disagrees with section {found_section.overnight_area_id}")
                    else:
                        errors.append(f"No overnight section covers date {visit.date}")

    return errors

def m1_planning_node(state: WorkflowState) -> WorkflowState:
    if "execution_summaries" not in state:
        state["execution_summaries"] = []
        
    start_time = datetime.utcnow()
    
    try:
        # Read from snapshot
        snapshot = state.get("input_snapshot", {})
        start_date_str = snapshot.get("StartDate")
        end_date_str = snapshot.get("EndDate")
        group_size = snapshot.get("GroupSize")
        budget = snapshot.get("Budget")
        interests = snapshot.get("Interests")
        destinations = snapshot.get("Destinations", [])
        
        if not start_date_str or not end_date_str:
            raise ValueError("StartDate or EndDate missing in snapshot.")
            
        start_date = parse_date(start_date_str)
        end_date = parse_date(end_date_str)
        
        # 3. Read real area IDs and names through the existing scoped planning-context tool.
        client = InternalAgentClient(proposal_id=state.get("proposal_id"))
        trip_context = client.get_trip()
        
        if not trip_context:
            raise ValueError("Failed to retrieve trip context from backend.")
            
        # Extract destination mapping from the live context (ItineraryItems)
        itinerary_items = trip_context.get("itineraryItems", [])
        dest_map = {item["destinationId"]: item["destinationName"] for item in itinerary_items}
        
        # Enrich snapshot destinations with names
        enriched_destinations = []
        for d in destinations:
            did = d["DestinationId"]
            name = dest_map.get(did, f"Area {did}")
            enriched_destinations.append({"DestinationId": did, "DestinationName": name, "DayNumber": d.get("DayNumber")})
            
        # Validate required inputs
        if end_date < start_date:
            raise ValueError("EndDate precedes StartDate.")
            
        prompt = f"""Generate a trip plan based on the following context:
Start Date: {start_date_str}
End Date: {end_date_str}
Group Size: {group_size}
Budget: {budget} LKR
Interests: {interests}

Selected Areas: {json.dumps(enriched_destinations)}

Ensure you follow the critical rules provided in the system instructions.
Return the plan structured as JSON according to the schema.
"""
        
        llm = get_gemini_client()
        
        # Attempt generation
        max_attempts = 2
        last_errors = []
        final_plan = None
        
        for attempt in range(max_attempts):
            if attempt > 0:
                correction_prompt = prompt + f"\n\nPrevious attempt failed validation with errors:\n{json.dumps(last_errors)}\nPlease correct these errors and generate a valid plan."
                plan = llm.generate_structured(M1_SYSTEM_PROMPT + "\n\n" + correction_prompt, TripPlan)
            else:
                plan = llm.generate_structured(M1_SYSTEM_PROMPT + "\n\n" + prompt, TripPlan)
                
            errors = validate_plan(plan, start_date, end_date, enriched_destinations)
            if not errors:
                final_plan = plan
                break
            last_errors = errors
            
        if not final_plan:
            raise ValueError(f"Failed to generate a valid plan after {max_attempts} attempts. Last errors: {last_errors}")
            
        state["plan"] = final_plan.model_dump()
        
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m1_planning",
            status="Success",
            finalOutcome="Pass",
            resultSummary=json.dumps({"message": "Successfully generated valid TripPlan", "daily_visits": len(final_plan.daily_visits)}),
            startedAt=start_time,
            completedAt=datetime.utcnow()
        ))
        
    except Exception as e:
        state["plan"] = None
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m1_planning",
            status="AgentFailed",
            finalOutcome="Fail",
            errors=json.dumps({"error": str(e)}),
            startedAt=start_time,
            completedAt=datetime.utcnow()
        ))

    return state
