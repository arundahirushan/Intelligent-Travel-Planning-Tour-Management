import pytest
import datetime
from unittest.mock import patch, MagicMock
from state.workflow_state import WorkflowState
from agents.m3_transport_weather import m3_transport_weather_node

@pytest.fixture
def mock_client():
    with patch("agents.m3_transport_weather.InternalAgentClient") as MockClient:
        client = MockClient.return_value
        # Default mock responses
        client.search_vehicles.return_value = [
            {"vehicleId": 101, "model": "Van", "pricePerDay": 10000, "capacity": 6},
            {"vehicleId": 102, "model": "Car", "pricePerDay": 5000, "capacity": 4},
            {"vehicleId": 103, "model": "Luxury Van", "pricePerDay": 15000, "capacity": 8}
        ]
        client.get_weather.return_value = {
            "destination": "Colombo",
            "date": "2026-10-01",
            "status": "Available",
            "advisory": "Generally clear conditions are forecast.",
            "WeatherCode": 1,
            "MaxTemperatureC": 30.0
        }
        yield client

@pytest.fixture
def mock_gemini():
    with patch("agents.m3_transport_weather.genai.Client") as MockGenaiClient:
        ai_client = MockGenaiClient.return_value
        response_mock = MagicMock()
        response_mock.parsed.selected_vehicle_id = 102 # The cheapest
        response_mock.parsed.explanation = "It fits the group and budget perfectly."
        ai_client.models.generate_content.return_value = response_mock
        yield ai_client

@pytest.fixture
def base_state():
    return WorkflowState({
        "proposal_id": "test_123",
        "trip_id": 1,
        "input_snapshot": {
            "StartDate": "2026-10-01T00:00:00Z",
            "EndDate": "2026-10-03T00:00:00Z",
            "GroupSize": 3,
            "PickupLatitude": 6.9271,
            "PickupLongitude": 79.8612
        },
        "accommodation_summary": {
            "cost": 20000,
            "remaining_budget": 50000
        },
        "plan": {
            "daily_visits": [
                {"date": "2026-10-01", "destination_name": "Colombo"},
                {"date": "2026-10-02", "destination_name": "Kandy"}
            ]
        },
        "execution_summaries": []
    })

def test_m3_successful_transport_and_weather(base_state, mock_client, mock_gemini):
    # Days = 2 (Oct 1 to Oct 3 is 2 days)
    new_state = m3_transport_weather_node(base_state)
    
    summaries = new_state["execution_summaries"]
    assert len(summaries) == 1
    assert summaries[0].status == "Success"
    
    assert len(new_state["vehicles"]) == 1
    vehicle = new_state["vehicles"][0]
    assert vehicle["VehicleId"] == 102
    assert vehicle["Cost"] == 10000 # 5000 * 2
    
    assert "transport_summary" in new_state
    
    assert len(new_state["weather"]) == 2

def test_m3_fails_if_no_pickup(base_state, mock_client, mock_gemini):
    del base_state["input_snapshot"]["PickupLatitude"]
    new_state = m3_transport_weather_node(base_state)
    assert new_state["execution_summaries"][-1].status == "AgentFailed"
    assert "Pickup location is missing" in new_state["execution_summaries"][-1].errors

def test_m3_fails_if_over_budget(base_state, mock_client, mock_gemini):
    # Reduce budget so even the cheapest (10000) exceeds it
    base_state["accommodation_summary"]["remaining_budget"] = 5000
    new_state = m3_transport_weather_node(base_state)
    assert new_state["execution_summaries"][-1].status == "AgentFailed"
    assert "No valid vehicles found" in new_state["execution_summaries"][-1].errors

def test_m3_weather_failure_does_not_fail_agent(base_state, mock_client, mock_gemini):
    mock_client.get_weather.side_effect = Exception("API limit reached")
    new_state = m3_transport_weather_node(base_state)
    
    # Agent still succeeds
    assert new_state["execution_summaries"][-1].status == "Success"
    
    # Weather just records the failure
    weather = new_state["weather"]
    assert len(weather) == 2
    assert weather[0]["status"] == "Unavailable_ProviderError"

def test_m3_fails_if_gemini_hallucinates(base_state, mock_client, mock_gemini):
    # Make gemini hallucinate an invalid ID
    mock_gemini.models.generate_content.return_value.parsed.selected_vehicle_id = 999
    
    new_state = m3_transport_weather_node(base_state)
    
    # It should have retried (called twice)
    assert mock_gemini.models.generate_content.call_count == 2
    
    # And failed
    assert new_state["execution_summaries"][-1].status == "AgentFailed"
    assert "Gemini selected an invalid" in new_state["execution_summaries"][-1].errors

