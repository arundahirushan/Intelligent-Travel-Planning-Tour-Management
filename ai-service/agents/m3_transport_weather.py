import datetime
import json
from state.workflow_state import WorkflowState
from schemas.models import AgentExecutionSummary
from tools.client import InternalAgentClient
from google import genai
import os
from pydantic import BaseModel, Field

# Ensure we have the schema models needed
class VehicleRecommendation(BaseModel):
    selected_vehicle_id: int = Field(..., description="The ID of the chosen vehicle.")
    explanation: str = Field(..., description="A concise explanation for this recommendation.")

def m3_transport_weather_node(state: WorkflowState) -> WorkflowState:
    summary = AgentExecutionSummary(
        agentIdentity="m3_transport_weather",
        status="InProgress",
        finalOutcome="Unknown"
    )
    
    try:
        client = InternalAgentClient(
            proposal_id=state["proposal_id"],
            ai_secret=os.environ.get("AI_SECRET", "dev-secret-do-not-use-in-prod")
        )
        snapshot = state.get("input_snapshot", {})
        pickup_lat = snapshot.get("PickupLatitude")
        pickup_lon = snapshot.get("PickupLongitude")
        
        if pickup_lat is None or pickup_lon is None:
            summary.status = "AgentFailed"
            summary.finalOutcome = "Failed"
            summary.errors = json.dumps(["Pickup location is missing. A valid pickup location is required."])
            state["execution_summaries"].append(summary)
            return state
            
        start_date_str = snapshot["StartDate"].split("T")[0]
        end_date_str = snapshot["EndDate"].split("T")[0]
        start_date = datetime.datetime.fromisoformat(start_date_str).date()
        end_date = datetime.datetime.fromisoformat(end_date_str).date()
        
        days = max(1, (end_date - start_date).days)
        
        acc_summary = state.get("accommodation_summary", {})
        remaining_budget = acc_summary.get("remaining_budget")
        
        if remaining_budget is None:
            summary.status = "AgentFailed"
            summary.finalOutcome = "Failed"
            summary.errors = json.dumps(["Missing accommodation_summary.remaining_budget from M2."])
            state["execution_summaries"].append(summary)
            return state
            
        vehicles = client.search_vehicles(snapshot["StartDate"], snapshot["EndDate"], snapshot["GroupSize"])
        
        valid_candidates = []
        for v in vehicles:
            cost = v["pricePerDay"] * days
            if cost <= remaining_budget:
                valid_candidates.append({**v, "totalCost": cost})
                
        if not valid_candidates:
            summary.status = "AgentFailed"
            summary.finalOutcome = "Failed"
            summary.errors = json.dumps(["No valid vehicles found within the remaining budget and capacity requirements."])
            state["execution_summaries"].append(summary)
            return state
            
        # Group by cost to find cheapest
        valid_candidates.sort(key=lambda x: x["totalCost"])
        min_cost = valid_candidates[0]["totalCost"]
        cheapest_vehicles = [v for v in valid_candidates if v["totalCost"] == min_cost]
        
        ai_client = genai.Client(api_key=os.environ.get("GEMINI_API_KEY"))
        
        prompt = f"""
        You are recommending a vehicle for a trip in Sri Lanka.
        The trip budget remaining for transport is {remaining_budget} LKR.
        The trip requires a vehicle for {days} rental days.
        
        Here are the cheapest available vehicles (Cost: {min_cost} LKR):
        {json.dumps(cheapest_vehicles, indent=2)}
        
        Select one vehicle from this exact list. Base tie-breaks on model preference, capacity, or common sense for the group size ({snapshot["GroupSize"]}). Provide a concise explanation (1-2 sentences).
        """
        
        def get_model_recommendation():
            response = ai_client.models.generate_content(
                model='gemini-3.5-flash-lite',
                contents=prompt,
                config=genai.types.GenerateContentConfig(
                    response_mime_type="application/json",
                    response_schema=VehicleRecommendation
                )
            )
            return response.parsed
            
        rec = get_model_recommendation()
        selected_v = next((v for v in cheapest_vehicles if v["vehicleId"] == rec.selected_vehicle_id), None)
        
        if not selected_v:
            # Retry once
            rec = get_model_recommendation()
            selected_v = next((v for v in cheapest_vehicles if v["vehicleId"] == rec.selected_vehicle_id), None)
            
        if not selected_v:
            summary.status = "AgentFailed"
            summary.finalOutcome = "Failed"
            summary.errors = json.dumps(["Gemini selected an invalid or unavailable vehicle after retry."])
            state["execution_summaries"].append(summary)
            return state
            
        state["vehicles"] = [{
            "VehicleId": selected_v["vehicleId"],
            "StartDate": snapshot["StartDate"],
            "EndDate": snapshot["EndDate"],
            "PickupLatitude": pickup_lat,
            "PickupLongitude": pickup_lon,
            "PickupNote": snapshot.get("PickupNote"),
            "Explanation": rec.explanation,
            "Cost": selected_v["totalCost"],
            "VehicleDetails": selected_v
        }]
        
        state["transport_summary"] = {
            "transport_cost": selected_v["totalCost"],
            "total_trip_cost": acc_summary.get("cost", 0) + selected_v["totalCost"],
            "remaining_budget": remaining_budget - selected_v["totalCost"],
            "currency": "LKR",
            "limitations": "Does not include fuel, tolls, or driver gratuity unless specified by the provider."
        }
        
        # Advisory Weather
        weather_results = []
        plan = state.get("plan", {})
        visits = plan.get("daily_visits", [])
        
        visited_pairs = set()
        for visit in visits:
            visit_date = visit.get("date")
            destinations = visit.get("visited_area_names", [])
            overnight = visit.get("overnight_area_name")
            destination_name = visit.get("destination_name")
            all_dests = set(destinations)
            if overnight:
                all_dests.add(overnight)
            if destination_name:
                all_dests.add(destination_name)
                
            if not visit_date or not all_dests:
                continue
                
            for dest in all_dests:
                pair = (visit_date, dest)
                if pair in visited_pairs: continue
                visited_pairs.add(pair)
                
                try:
                    w = client.get_weather(dest, visit_date)
                    weather_results.append(w)
                except Exception as e:
                    weather_results.append({
                        "destination": dest,
                        "date": visit_date,
                        "status": "Unavailable_ProviderError",
                        "advisory": f"Weather service unavailable: {str(e)}"
                    })
                
        state["weather"] = weather_results
        
        summary.status = "Success"
        summary.finalOutcome = "Pass"
        summary.resultSummary = json.dumps({
            "message": f"Selected vehicle {selected_v['vehicleId']} ({selected_v['model']}) for {selected_v['totalCost']} LKR.",
            "weather_pairs_retrieved": len(weather_results),
        })
        
    except Exception as e:
        summary.status = "AgentFailed"
        summary.finalOutcome = "Failed"
        summary.errors = json.dumps([str(e)])
        
    state["execution_summaries"].append(summary)
    return state
