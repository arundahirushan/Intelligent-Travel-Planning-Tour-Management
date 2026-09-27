from typing import TypedDict, List, Dict, Any, Optional
from schemas.models import AgentExecutionSummary

class WorkflowState(TypedDict):
    trip_id: int
    proposal_id: str
    input_snapshot: Dict[str, Any]
    
    # Internal state populated by agents
    plan: Optional[Dict[str, Any]]
    hotels: List[Dict[str, Any]]
    vehicles: List[Dict[str, Any]]
    weather: Optional[Dict[str, Any]]
    
    # Validations & outputs
    validation_errors: List[str]
    is_valid: bool
    final_payload: str # JSON representation of CreateCheckoutDto
    
    execution_summaries: List[AgentExecutionSummary]
