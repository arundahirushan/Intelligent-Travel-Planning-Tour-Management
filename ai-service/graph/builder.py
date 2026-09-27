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
    
    # Add edges
    builder.set_entry_point("planning")
    builder.add_edge("planning", "accommodation")
    builder.add_edge("accommodation", "transport_weather")
    builder.add_edge("transport_weather", "validation")
    builder.add_edge("validation", END)
    
    return builder.compile()

# Instantiate the graph
workflow_app = build_graph()
