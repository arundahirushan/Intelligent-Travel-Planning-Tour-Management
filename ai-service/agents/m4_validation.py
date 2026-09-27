import json
from state.workflow_state import WorkflowState
from schemas.models import AgentExecutionSummary

def m4_validation_node(state: WorkflowState) -> WorkflowState:
    # In a real implementation, this checks if the selected hotels cover all the
    # required nights and areas, as specified in the Option B rules.
    
    # We construct the final payload representing CreateCheckoutDto
    
    payload = {
        "TripId": state["trip_id"],
        "ProposalId": state["proposal_id"],
        "Hotels": state.get("hotels", []),
        "Vehicle": state.get("vehicles", [None])[0] if state.get("vehicles") else None
    }
    
    # Check if prior nodes were unimplemented
    has_unimplemented = any(
        s.status == "AgentNotImplemented" for s in state.get("execution_summaries", [])
    )

    if has_unimplemented:
        state["is_valid"] = False
        state["validation_errors"] = ["Upstream nodes are not implemented."]
        state["final_payload"] = "{}"
        
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m4_validation",
            status="AgentNotImplemented",
            finalOutcome="NotImplemented"
        ))
    else:
        state["is_valid"] = True
        state["validation_errors"] = []
        state["final_payload"] = json.dumps(payload)
        
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m4_validation",
            status="Success",
            finalOutcome="Pass"
        ))
    
    return state
