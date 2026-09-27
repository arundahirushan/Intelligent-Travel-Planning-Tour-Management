import json
from state.workflow_state import WorkflowState
from schemas.models import AgentExecutionSummary

def m4_validation_node(state: WorkflowState) -> WorkflowState:
    # In a real implementation, this checks if the selected hotels cover all the
    # required nights and areas, as specified in the Option B rules.
    
    # We construct the final payload representing CreateCheckoutDto
    
    checkout_hotels = []
    for section in state.get("hotels", []):
        for line in section.get("RoomLines", []):
            checkout_hotels.append({
                "RoomId": line["RoomId"],
                "CheckInDate": section["CheckInDate"],
                "CheckOutDate": section["CheckOutDate"],
                "NumberOfRooms": line["Quantity"]
            })
    
    payload = {
        "TripId": state["trip_id"],
        "ProposalId": state["proposal_id"],
        "Hotels": checkout_hotels,
        "Vehicle": state.get("vehicles", [None])[0] if state.get("vehicles") else None,
        "PartialPlan": state.get("plan"),
        "PartialAccommodation": state.get("accommodation_summary"),
        "PartialTransport": state.get("transport_summary"),
        "PartialWeather": state.get("weather")
    }

    # Check if prior nodes were unimplemented or failed
    has_unimplemented = any(
        s.status == "AgentNotImplemented" for s in state.get("execution_summaries", [])
    )
    has_failed = any(
        s.status == "AgentFailed" for s in state.get("execution_summaries", [])
    )

    state["final_payload"] = json.dumps(payload)

    if has_failed:
        state["is_valid"] = False
        state["validation_errors"] = ["Upstream nodes failed."]
        
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m4_validation",
            status="AgentFailed",
            finalOutcome="Failed"
        ))
    elif has_unimplemented:
        state["is_valid"] = False
        state["validation_errors"] = ["Upstream nodes are not implemented."]
        
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m4_validation",
            status="AgentNotImplemented",
            finalOutcome="NotImplemented"
        ))
    else:
        # M4 itself is unfinished, so even if M1-M3 succeed, we must not falsely approve.
        state["is_valid"] = False
        state["validation_errors"] = ["M4 validation is not yet implemented."]
        
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m4_validation",
            status="AgentNotImplemented",
            finalOutcome="NotImplemented"
        ))
    
    return state
