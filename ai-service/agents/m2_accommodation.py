import json
from datetime import datetime
from collections import defaultdict
from state.workflow_state import WorkflowState
from schemas.models import AgentExecutionSummary, AccommodationSelection, SelectedRoomLine
from agents.llm_client import get_gemini_client
from tools.client import InternalAgentClient
from pydantic import BaseModel
from typing import List

M2_SYSTEM_PROMPT = """You are Member 2 (M2), the Accommodation Analysis agent for an Intelligent Travel Planning system.
Your job is to select the most appropriate hotel and room allocation for each overnight section of a planned trip.

CRITICAL RULES:
1. Affordable-hotels-first policy: Choose the lowest-cost valid accommodation that satisfies the requirements.
2. If multiple allocations have the same cost, break ties using StarRating or Amenities (e.g. preference for better ratings or relevant amenities).
3. Do NOT invent new hotels, room types, or prices. You MUST select exactly from the provided Candidates.
4. Provide a clear explanation for your selection, emphasizing the affordable-first policy.
5. If no candidates are provided for an overnight section, you cannot select one. The system will handle this failure.
"""

class M2LLMSelection(BaseModel):
    section_index: int
    hotel_id: int
    allocation_index: int
    explanation: str

class M2LLMResponse(BaseModel):
    selections: List[M2LLMSelection]

def get_hotel_allocations(rooms: list[dict], group_size: int):
    """
    Finds combinations of rooms within a single hotel that can accommodate the group size.
    Uses backtracking to evaluate combinations rather than greedy selection, 
    ensuring we do not miss a cheaper combination (e.g. 1 triple + 1 double instead of 3 doubles).
    """
    valid_allocations = []
    
    def backtrack(room_idx, current_qty_map, current_capacity, current_cost):
        if current_capacity >= group_size:
            valid_allocations.append({
                "allocation": dict(current_qty_map),
                "capacity": current_capacity,
                "cost": current_cost
            })
            return
            
        if room_idx >= len(rooms):
            return
            
        room = rooms[room_idx]
        room_id = room.get("RoomId") or room.get("roomId")
        max_qty = room.get("AvailableRoomCount") or room.get("availableRoomCount") or 0
        capacity = room.get("Capacity") or room.get("capacity") or 0
        price_per_night = room.get("PricePerNight") or room.get("pricePerNight") or 0.0
        
        for qty in range(max_qty + 1):
            if qty > 0:
                current_qty_map[room_id] = qty
            
            backtrack(
                room_idx + 1, 
                current_qty_map, 
                current_capacity + (qty * capacity), 
                current_cost + (qty * price_per_night)
            )
            
            if qty > 0:
                del current_qty_map[room_id]

    backtrack(0, {}, 0, 0.0)
    valid_allocations.sort(key=lambda x: (x["cost"], x["capacity"]))
    return valid_allocations[:3]

def m2_accommodation_node(state: WorkflowState) -> WorkflowState:
    start_time = datetime.utcnow()
    
    try:
        plan = state.get("plan")
        if not plan:
            raise ValueError("M1 plan is missing. Upstream planning must complete successfully.")
            
        snapshot = state.get("input_snapshot", {})
        group_size = snapshot.get("GroupSize")
        budget = snapshot.get("Budget")
        
        start_date_str = snapshot.get("StartDate")
        end_date_str = snapshot.get("EndDate")
        
        if not group_size or not budget or not start_date_str or not end_date_str:
            raise ValueError("GroupSize, Budget, StartDate, or EndDate missing from input snapshot.")

        overnight_sections = plan.get("overnight_sections", [])
        is_one_day = start_date_str == end_date_str
        
        if is_one_day:
            if overnight_sections:
                raise ValueError("One-day trip must not have overnight sections.")
                
            state["hotels"] = []
            state["accommodation_summary"] = {
                "cost": 0.0,
                "currency": "LKR",
                "remaining_budget": budget
            }
            
            state["execution_summaries"].append(AgentExecutionSummary(
                agentIdentity="m2_accommodation",
                status="Success",
                finalOutcome="Pass",
                resultSummary=json.dumps({"message": "No accommodation required for one-day trip.", "cost": 0.0}),
                startedAt=start_time,
                completedAt=datetime.utcnow()
            ))
            return state
        else:
            if not overnight_sections:
                raise ValueError("Overnight trip must have overnight sections.")

        client = InternalAgentClient(proposal_id=state.get("proposal_id"))
        
        all_candidates = []
        
        # 1. Gather all candidates for each section
        for i, section in enumerate(overnight_sections):
            dest_id = section["overnight_area_id"]
            check_in = section["check_in_date"]
            check_out = section["check_out_date"]
            
            # Fetch available rooms
            search_results = client.search_hotels(
                destination_id=dest_id,
                check_in=check_in,
                check_out=check_out,
                group_size=group_size
            )
            
            # Group by hotel
            hotel_map = defaultdict(list)
            for room in search_results:
                hotel_id = room.get("HotelId") or room.get("hotelId")
                hotel_map[hotel_id].append(room)
                
            section_candidates = []
            
            for hotel_id, rooms in hotel_map.items():
                allocations = get_hotel_allocations(rooms, group_size)
                if allocations:
                    hotel_name = rooms[0].get("HotelName") or rooms[0].get("hotelName")
                    star_rating = rooms[0].get("StarRating") or rooms[0].get("starRating")
                    section_candidates.append({
                        "hotel_id": hotel_id,
                        "hotel_name": hotel_name,
                        "star_rating": star_rating,
                        "allocations": allocations,
                        "rooms_info": {(r.get("RoomId") or r.get("roomId")): r for r in rooms}
                    })
                    
            if not section_candidates:
                raise ValueError(f"No suitable accommodation found for section {i} in area {dest_id} ({check_in} to {check_out}).")
                
            all_candidates.append(section_candidates)
            
        # 2. Ask Gemini to select the best candidate per section based on policy
        llm = get_gemini_client()
        prompt = f"Trip requirements: Group size: {group_size}, Total Trip Budget: {budget} LKR.\n\n"
        prompt += "Candidate Accommodations per Section:\n"
        
        for i, (section, candidates) in enumerate(zip(overnight_sections, all_candidates)):
            prompt += f"\nSection {i}: {section['overnight_area_name']} ({section['check_in_date']} to {section['check_out_date']}, {section['night_count']} nights)\n"
            for c in candidates:
                prompt += f"  Hotel ID: {c['hotel_id']} ({c['hotel_name']}, {c['star_rating']} stars)\n"
                for a_idx, alloc in enumerate(c["allocations"]):
                    prompt += f"    Allocation {a_idx}: {alloc['allocation']}, Total Cost (1 night): {alloc['cost']}, Capacity: {alloc['capacity']}\n"
                    
        prompt += "\nSelect exactly one hotel and allocation for each section following the affordable-hotels-first policy. Provide your reasoning."
        
        llm_response = llm.generate_structured(M2_SYSTEM_PROMPT + "\n\n" + prompt, M2LLMResponse)
        
        # 3. Process LLM selections and build final structures
        final_hotels = []
        total_accommodation_cost = 0.0
        
        if len(llm_response.selections) != len(overnight_sections):
            raise ValueError("LLM did not provide a selection for every overnight section.")
            
        for i, selection in enumerate(llm_response.selections):
            section = overnight_sections[i]
            candidates = all_candidates[i]
            
            # Find the chosen hotel and allocation
            chosen_candidate = next((c for c in candidates if c["hotel_id"] == selection.hotel_id), None)
            if not chosen_candidate:
                raise ValueError(f"LLM selected invalid hotel_id {selection.hotel_id} for section {i}.")
                
            if selection.allocation_index < 0 or selection.allocation_index >= len(chosen_candidate["allocations"]):
                raise ValueError(f"LLM selected invalid allocation_index {selection.allocation_index} for section {i}.")
                
            alloc = chosen_candidate["allocations"][selection.allocation_index]
            
            # Enforce affordable-first in code
            min_cost_for_section = min(
                a["cost"] 
                for c in candidates 
                for a in c["allocations"]
            )
            
            if alloc["cost"] > min_cost_for_section:
                raise ValueError(f"LLM selected allocation costing {alloc['cost']} but a cheaper valid allocation costing {min_cost_for_section} exists. Affordable-first policy violated.")
                
            night_count = section["night_count"]
            section_total_cost = alloc["cost"] * night_count
            total_accommodation_cost += section_total_cost
            
            room_lines = []
            for room_id, qty in alloc["allocation"].items():
                room_info = chosen_candidate["rooms_info"][room_id]
                room_type = room_info.get("RoomType") or room_info.get("roomType")
                room_cap = room_info.get("Capacity") or room_info.get("capacity") or 0
                room_price = room_info.get("PricePerNight") or room_info.get("pricePerNight") or 0.0
                room_lines.append(SelectedRoomLine(
                    RoomId=room_id,
                    RoomType=room_type,
                    Quantity=qty,
                    CapacityPerRoom=room_cap,
                    PricePerNight=room_price,
                    LineCost=room_price * qty * night_count
                ))
                
            acc_sel = AccommodationSelection(
                OvernightAreaId=section["overnight_area_id"],
                OvernightAreaName=section["overnight_area_name"],
                CheckInDate=section["check_in_date"],
                CheckOutDate=section["check_out_date"],
                NightCount=night_count,
                HotelId=chosen_candidate["hotel_id"],
                HotelName=chosen_candidate["hotel_name"],
                RoomLines=room_lines,
                SectionCapacity=alloc["capacity"],
                SectionCost=section_total_cost,
                Explanation=selection.explanation
            )
            final_hotels.append(acc_sel.model_dump())
            
        # 4. Enforce Budget constraints
        remaining_budget = budget - total_accommodation_cost
        if total_accommodation_cost > budget:
            raise ValueError(f"Accommodation cost ({total_accommodation_cost}) exceeds the total trip budget ({budget}). NeedsRevision.")
            
        state["hotels"] = final_hotels
        state["accommodation_summary"] = {
            "cost": total_accommodation_cost,
            "currency": "LKR",
            "remaining_budget": remaining_budget
        }
        
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m2_accommodation",
            status="Success",
            finalOutcome="Pass",
            resultSummary=json.dumps({"cost": total_accommodation_cost, "remaining_budget": remaining_budget}),
            startedAt=start_time,
            completedAt=datetime.utcnow()
        ))
        
    except Exception as e:
        state["hotels"] = []
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m2_accommodation",
            status="AgentFailed",
            finalOutcome="Fail",
            errors=json.dumps({"error": str(e)}),
            startedAt=start_time,
            completedAt=datetime.utcnow()
        ))
        
    return state
