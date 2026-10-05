import pytest
from unittest.mock import patch, MagicMock
from state.workflow_state import WorkflowState
from graph.builder import build_graph
from schemas.models import AgentExecutionSummary

def create_mock_node(name, status="Success"):
    def mock_node(state):
        summary = AgentExecutionSummary(
            agentIdentity=name,
            status=status,
            finalOutcome="Pass" if status == "Success" else "Failed"
        )
        if "execution_summaries" not in state or state["execution_summaries"] is None:
            state["execution_summaries"] = []
        state["execution_summaries"].append(summary)
        return state
    return mock_node

def test_routing_m1_failure_skips_m2_m3():
    with patch("graph.builder.m1_planning_node", side_effect=create_mock_node("m1", "AgentFailed")), \
         patch("graph.builder.m2_accommodation_node", side_effect=create_mock_node("m2")), \
         patch("graph.builder.m3_transport_weather_node", side_effect=create_mock_node("m3")), \
         patch("graph.builder.m4_validation_node", side_effect=create_mock_node("m4")):
        
        graph = build_graph()
        result = graph.invoke({"proposal_id": "1", "trip_id": 1, "input_snapshot": {}, "execution_summaries": []})
        
        visited = [s.agentIdentity for s in result["execution_summaries"]]
        assert visited == ["m1", "m4"]

def test_routing_m2_failure_skips_m3():
    with patch("graph.builder.m1_planning_node", side_effect=create_mock_node("m1", "Success")), \
         patch("graph.builder.m2_accommodation_node", side_effect=create_mock_node("m2", "AgentFailed")), \
         patch("graph.builder.m3_transport_weather_node", side_effect=create_mock_node("m3")), \
         patch("graph.builder.m4_validation_node", side_effect=create_mock_node("m4")):
        
        graph = build_graph()
        result = graph.invoke({"proposal_id": "1", "trip_id": 1, "input_snapshot": {}, "execution_summaries": []})
        
        visited = [s.agentIdentity for s in result["execution_summaries"]]
        assert visited == ["m1", "m2", "m4"]

def test_routing_m3_failure_reaches_m4():
    with patch("graph.builder.m1_planning_node", side_effect=create_mock_node("m1", "Success")), \
         patch("graph.builder.m2_accommodation_node", side_effect=create_mock_node("m2", "Success")), \
         patch("graph.builder.m3_transport_weather_node", side_effect=create_mock_node("m3", "AgentFailed")), \
         patch("graph.builder.m4_validation_node", side_effect=create_mock_node("m4")):
        
        graph = build_graph()
        result = graph.invoke({"proposal_id": "1", "trip_id": 1, "input_snapshot": {}, "execution_summaries": []})
        
        visited = [s.agentIdentity for s in result["execution_summaries"]]
        assert visited == ["m1", "m2", "m3", "m4"]

def test_routing_success_reaches_m4():
    with patch("graph.builder.m1_planning_node", side_effect=create_mock_node("m1", "Success")), \
         patch("graph.builder.m2_accommodation_node", side_effect=create_mock_node("m2", "Success")), \
         patch("graph.builder.m3_transport_weather_node", side_effect=create_mock_node("m3", "Success")), \
         patch("graph.builder.m4_validation_node", side_effect=create_mock_node("m4")):
        
        graph = build_graph()
        result = graph.invoke({"proposal_id": "1", "trip_id": 1, "input_snapshot": {}, "execution_summaries": []})
        
        visited = [s.agentIdentity for s in result["execution_summaries"]]
        assert visited == ["m1", "m2", "m3", "m4"]
