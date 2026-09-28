# Live Check Script for M3

To run live checks against the backend, ensure the ASP.NET Core server is running, and you have configured `.env` correctly.

## 1. Setup

```bash
cd server/TourManagement.Api
dotnet run
```

In a new terminal:
```bash
cd ai-service
source .venv/bin/activate
pip install -r requirements.txt
export GEMINI_API_KEY="your-gemini-key"
export INTERNAL_API_SECRET="your-configured-secret"
export TOUR_MANAGEMENT_API_URL="http://localhost:5032/api/internal"
```

## 2. Testing M3 Execution (Smoke Test)

You can run a script to isolate M3 testing. Create `smoke_test_m3.py`:

```python
import os
import json
from agents.m3_transport_weather import m3_transport_weather_node
from state.workflow_state import WorkflowState

state = WorkflowState({
    "proposal_id": "test_123",
    "trip_id": 1,
    "input_snapshot": {
        "StartDate": "2026-10-01T00:00:00Z",
        "EndDate": "2026-10-03T00:00:00Z",
        "GroupSize": 3,
        "PickupLatitude": 6.9271,
        "PickupLongitude": 79.8612
    },
    "accommodation_summary": {
        "cost": 20000,
        "remaining_budget": 100000
    },
    "plan": {
        "daily_visits": [
            {"date": "2026-10-01", "destination_name": "Colombo"},
            {"date": "2026-10-02", "destination_name": "Kandy"}
        ]
    },
    "execution_summaries": []
})

result = m3_transport_weather_node(state)
print(json.dumps(result["vehicles"], indent=2))
print(json.dumps(result["weather"], indent=2))
print(json.dumps([s.model_dump() for s in result["execution_summaries"]], indent=2))
```

Run it via:
```bash
python smoke_test_m3.py
```

## 3. End to End (M1 -> M2 -> M3 -> M4)
The current LangGraph orchestration uses `conditional_edges` to automatically route failures directly to M4. If M3 succeeds, the vehicles, weather, and execution summary are appended to the workflow state, serialized as JSON by M4, and stored in the database. 

