"""
state/workflow_state.py — Shared workflow state (placeholder)

Defines the TypedDict / Pydantic model that is passed between all agent nodes
in the LangGraph StateGraph. Every agent reads from and writes to this state
object, which is persisted at each step of the pipeline.

Fields will be added in a later prompt when the agent logic is implemented.
"""

from typing import Any, Optional
# from pydantic import BaseModel
# from typing import TypedDict

# class WorkflowState(TypedDict):
#     trip_id: str
#     user_id: str
#     itinerary: Optional[dict]
#     recommendations: Optional[dict]
#     weather_data: Optional[dict]
#     validation_result: Optional[dict]
#     status: str
#     errors: list[str]
