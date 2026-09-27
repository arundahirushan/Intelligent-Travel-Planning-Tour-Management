import os
import json
from datetime import datetime
from dotenv import load_dotenv
from agents.m2_accommodation import m2_accommodation_node
from state.workflow_state import WorkflowState
from schemas.models import TripPlan

load_dotenv()

def run_smoke_test():
    if not os.environ.get("GEMINI_API_KEY"):
        print("Skipping live smoke test: GEMINI_API_KEY is not set.")
        return

    print("Running live Gemini smoke test for M2...")
    
    plan = {
        "daily_visits": [],
        "overnight_sections": [
            {
                "overnight_area_id": 1,
                "overnight_area_name": "Kandy Area",
                "check_in_date": "2026-10-01",
                "check_out_date": "2026-10-03",
                "night_count": 2
            }
        ],
        "planning_summary": {
            "explanation": "test",
            "warnings_or_limitations": []
        }
    }
    
    dummy_state: WorkflowState = {
        "trip_id": 999,
        "proposal_id": "test-proposal-123",
        "input_snapshot": {
            "StartDate": "2026-10-01",
            "EndDate": "2026-10-03",
            "GroupSize": 5,
            "Budget": 150000.0,
        },
        "plan": plan,
        "hotels": [],
        "vehicles": [],
        "weather": None,
        "validation_errors": [],
        "is_valid": False,
        "final_payload": "{}",
        "execution_summaries": []
    }
    
    # Mock InternalAgentClient inside m2_accommodation to return fixture inventory
    import agents.m2_accommodation
    class MockClient:
        def __init__(self, *args, **kwargs):
            pass
        def search_hotels(self, destination_id, check_in, check_out, group_size):
            # Return some fake rooms
            return [
                {"HotelId": 10, "HotelName": "Grand Kandy", "StarRating": 4, "RoomId": 101, "RoomType": "Double", "PricePerNight": 8000.0, "Capacity": 2, "AvailableRoomCount": 2, "DestinationId": 1, "Amenities": "WiFi"},
                {"HotelId": 10, "HotelName": "Grand Kandy", "StarRating": 4, "RoomId": 102, "RoomType": "Triple", "PricePerNight": 11000.0, "Capacity": 3, "AvailableRoomCount": 1, "DestinationId": 1, "Amenities": "WiFi"},
                {"HotelId": 11, "HotelName": "Cheap Inn", "StarRating": 2, "RoomId": 201, "RoomType": "Dorm", "PricePerNight": 2000.0, "Capacity": 4, "AvailableRoomCount": 1, "DestinationId": 1, "Amenities": "None"}
            ]
            
    agents.m2_accommodation.InternalAgentClient = MockClient

    final_state = m2_accommodation_node(dummy_state)
    
    print("\n--- Execution Summaries ---")
    for es in final_state.get("execution_summaries", []):
        print(f"Agent: {es.agentIdentity}, Status: {es.status}, Outcome: {es.finalOutcome}")
        if es.errors:
            print(f"Errors: {es.errors}")
            
    print("\n--- Hotels Selected ---")
    hotels = final_state.get("hotels", [])
    if hotels:
        print(json.dumps(hotels, indent=2))
    else:
        print("No hotels selected.")

    print("\n--- Remaining Budget ---")
    print(final_state.get("accommodation_summary", {}).get("remaining_budget"))

if __name__ == "__main__":
    run_smoke_test()
