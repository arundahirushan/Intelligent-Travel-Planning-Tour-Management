import os
from fastapi import FastAPI, Depends, HTTPException, Header
from pydantic import BaseModel
from typing import Optional

from schemas.models import GenerateProposalRequest, AgentProposalResult, AgentExecutionSummary
from graph.builder import workflow_app

app = FastAPI(
    title="TourManagement AI Service",
    description="Internal-only LangGraph agent service. Called by TourManagement.Api only.",
    version="0.1.0",
)

AI_SECRET = os.environ.get("AI_SECRET", "dev-secret-do-not-use-in-prod")

def verify_ai_secret(x_ai_secret: Optional[str] = Header(None)):
    if not x_ai_secret or x_ai_secret != AI_SECRET:
        raise HTTPException(status_code=401, detail="Unauthorized internal access")

@app.get("/health")
def health_check():
    """Simple liveness probe so the API can verify the service is up."""
    return {"status": "ok"}

@app.post("/api/generate", response_model=AgentProposalResult, dependencies=[Depends(verify_ai_secret)])
async def generate_proposal(request: GenerateProposalRequest):
    # Initialize state
    initial_state = {
        "trip_id": request.tripId,
        "proposal_id": request.proposalId,
        "input_snapshot": request.inputSnapshot,
        "execution_summaries": []
    }
    
    # Run the graph
    try:
        final_state = workflow_app.invoke(initial_state)
        
        if final_state.get("is_valid"):
            status = "Generated"
        else:
            has_unimplemented = any(
                s.status == "AgentNotImplemented" for s in final_state.get("execution_summaries", [])
            )
            status = "GenerationFailed" if has_unimplemented else "NeedsInput"
        
        return AgentProposalResult(
            status=status,
            payload=final_state.get("final_payload", "{}"),
            executionSummaries=final_state.get("execution_summaries", [])
        )
    except Exception as e:
        return AgentProposalResult(
            status="GenerationFailed",
            payload='{"error": "Agent execution failed"}',
            executionSummaries=[]
        )
