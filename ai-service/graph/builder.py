from langgraph.graph import StateGraph, END
from state.workflow_state import WorkflowState
from agents.m1_planning import m1_planning_node
from agents.m2_accommodation import m2_accommodation_node
from agents.m3_transport_weather import m3_transport_weather_node
from agents.m4_validation import m4_validation_node

def build_graph():
    builder = StateGraph(WorkflowState)
    
    # Add nodes
    builder.add_node("planning", m1_planning_node)
    builder.add_node("accommodation", m2_accommodation_node)
    builder.add_node("transport_weather", m3_transport_weather_node)
    builder.add_node("validation", m4_validation_node)
    
    # Define conditional routing to skip to validation on failure
    def route_planning(state):
        if any(s.status == "AgentFailed" for s in state.get("execution_summaries", [])):
            return "validation"
        return "accommodation"
        
    def route_accommodation(state):
        if any(s.status == "AgentFailed" for s in state.get("execution_summaries", [])):
            return "validation"
        return "transport_weather"
        
    def route_transport(state):
        return "validation"

    # Add edges
    builder.set_entry_point("planning")
    builder.add_conditional_edges("planning", route_planning, {"accommodation": "accommodation", "validation": "validation"})
    builder.add_conditional_edges("accommodation", route_accommodation, {"transport_weather": "transport_weather", "validation": "validation"})
    builder.add_edge("transport_weather", "validation")
    builder.add_edge("validation", END)
    
    return builder.compile()

# Instantiate the graph
workflow_app = build_graph()
