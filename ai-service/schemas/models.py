from pydantic import BaseModel, Field
from typing import List, Optional, Any, Dict
from datetime import datetime

class GenerateProposalRequest(BaseModel):
    proposalId: str
    tripId: int
    inputSnapshot: Dict[str, Any]

class AgentExecutionSummary(BaseModel):
    agentIdentity: str
    status: str
    toolName: Optional[str] = None
    resultSummary: Optional[str] = None # JSON string
    validationResults: Optional[str] = None # JSON string
    errors: Optional[str] = None # JSON string
    retryCount: int = 0
    finalOutcome: str = ""
    startedAt: datetime = Field(default_factory=datetime.utcnow)
    completedAt: Optional[datetime] = None

class AgentProposalResult(BaseModel):
    status: str
    payload: str = "{}" # JSON string
    executionSummaries: List[AgentExecutionSummary] = Field(default_factory=list)
