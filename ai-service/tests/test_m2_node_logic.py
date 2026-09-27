import json
import pytest
from datetime import datetime
from state.workflow_state import WorkflowState
from agents.m2_accommodation import m2_accommodation_node, M2LLMSelection, M2LLMResponse

def test_m2_affordable_first_enforced():
    # Setup dummy state
    dummy_state = {
        "trip_id": 1,
        "proposal_id": "test",
        "input_snapshot": {
            "StartDate": "2026-10-01",
            "EndDate": "2026-10-02",
            "GroupSize": 2,
            "Budget": 50000.0,
        },
        "plan": {
            "overnight_sections": [
                {
                    "overnight_area_id": 1,
                    "overnight_area_name": "Test Area",
                    "check_in_date": "2026-10-01",
                    "check_out_date": "2026-10-02",
                    "night_count": 1
                }
            ]
        },
        "execution_summaries": []
    }
    
    # Mock client and LLM inside m2_accommodation
    import agents.m2_accommodation
    class MockClient:
        def __init__(self, *args, **kwargs):
            pass
        def search_hotels(self, destination_id, check_in, check_out, group_size):
            return [
                {"HotelId": 10, "HotelName": "Cheap", "StarRating": 2, "RoomId": 101, "RoomType": "Double", "PricePerNight": 2000.0, "Capacity": 2, "AvailableRoomCount": 1},
                {"HotelId": 20, "HotelName": "Expensive", "StarRating": 5, "RoomId": 201, "RoomType": "Double", "PricePerNight": 10000.0, "Capacity": 2, "AvailableRoomCount": 1}
            ]
            
    class MockLLMExpensive:
        def generate_structured(self, prompt, schema):
            # The LLM maliciously picks the expensive hotel!
            # Cheap is hotel 10, Expensive is hotel 20
            # Both have 1 valid allocation at index 0.
            return M2LLMResponse(selections=[
                M2LLMSelection(section_index=0, hotel_id=20, allocation_index=0, explanation="I like expensive hotels.")
            ])
            
    agents.m2_accommodation.InternalAgentClient = MockClient
    agents.m2_accommodation.get_gemini_client = lambda: MockLLMExpensive()
    
    result_state = m2_accommodation_node(dummy_state)
    
    # Verify it failed
    assert len(result_state["execution_summaries"]) == 1
    summary = result_state["execution_summaries"][0]
    assert summary.status == "AgentFailed"
    assert "Affordable-first policy violated" in summary.errors
    
def test_m2_one_day_trip():
    dummy_state = {
        "trip_id": 1,
        "proposal_id": "test",
        "input_snapshot": {
            "StartDate": "2026-10-01",
            "EndDate": "2026-10-01", # Same day = one-day trip
            "GroupSize": 2,
            "Budget": 50000.0,
        },
        "plan": {
            "overnight_sections": [] # Should be empty
        },
        "execution_summaries": []
    }
    result_state = m2_accommodation_node(dummy_state)
    summary = result_state["execution_summaries"][0]
    assert summary.status == "Success"
    assert result_state["accommodation_summary"]["cost"] == 0.0
    assert result_state["accommodation_summary"]["remaining_budget"] == 50000.0
    
def test_m2_overnight_trip_missing_sections():
    dummy_state = {
        "trip_id": 1,
        "proposal_id": "test",
        "input_snapshot": {
            "StartDate": "2026-10-01",
            "EndDate": "2026-10-02", # Overnight trip
            "GroupSize": 2,
            "Budget": 50000.0,
        },
        "plan": {
            "overnight_sections": [] # Missing sections!
        },
        "execution_summaries": []
    }
    result_state = m2_accommodation_node(dummy_state)
    summary = result_state["execution_summaries"][0]
    assert summary.status == "AgentFailed"
    assert "Overnight trip must have overnight sections" in summary.errors
