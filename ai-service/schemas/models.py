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

class DailyVisit(BaseModel):
    day_number: int
    date: str
    visited_area_ids: List[int]
    visited_area_names: List[str]
    overnight_area_id: Optional[int]
    overnight_area_name: Optional[str]
    explanation: str

class OvernightSection(BaseModel):
    overnight_area_id: int
    overnight_area_name: str
    check_in_date: str
    check_out_date: str
    night_count: int

class PlanningSummary(BaseModel):
    explanation: str
    warnings_or_limitations: List[str]

class TripPlan(BaseModel):
    daily_visits: List[DailyVisit]
    overnight_sections: List[OvernightSection]
    planning_summary: PlanningSummary

class SelectedRoomLine(BaseModel):
    RoomId: int
    RoomType: str
    Quantity: int
    CapacityPerRoom: int
    PricePerNight: float
    LineCost: float

class AccommodationSelection(BaseModel):
    OvernightAreaId: int
    OvernightAreaName: str
    CheckInDate: str
    CheckOutDate: str
    NightCount: int
    HotelId: int
    HotelName: str
    RoomLines: List[SelectedRoomLine]
    SectionCapacity: int
    SectionCost: float
    Explanation: str
