import os
import json
from datetime import datetime
from dotenv import load_dotenv
from agents.m1_planning import m1_planning_node
from state.workflow_state import WorkflowState

load_dotenv()

def run_smoke_test():
    if not os.environ.get("GEMINI_API_KEY"):
        print("Skipping live smoke test: GEMINI_API_KEY is not set.")
        return

    print("Running live Gemini smoke test for M1...")
    
    # Dummy state
    dummy_state: WorkflowState = {
        "trip_id": 999,
        "proposal_id": "test-proposal-123",
        "input_snapshot": {
            "StartDate": "2026-10-01",
            "EndDate": "2026-10-03",
            "GroupSize": 2,
            "Budget": 150000.0,
            "Interests": "Culture and wildlife",
            "Destinations": [
                {"DestinationId": 1, "DayNumber": 1},
                {"DestinationId": 2, "DayNumber": 2}
            ]
        },
        "plan": None,
        "hotels": [],
        "vehicles": [],
        "weather": None,
        "validation_errors": [],
        "is_valid": False,
        "final_payload": "{}",
        "execution_summaries": []
    }
    
    # Mocking the client since we don't have a live backend easily available in this exact test script
    # We will inject a mock client class into m1_planning_node's scope or just mock it here.
    # To keep it simple, we patch InternalAgentClient
    import agents.m1_planning
    class MockClient:
        def __init__(self, *args, **kwargs):
            pass
        def get_trip(self):
            return {
                "itineraryItems": [
                    {"destinationId": 1, "destinationName": "Kandy Area"},
                    {"destinationId": 2, "destinationName": "Galle Area"}
                ]
            }
    
    agents.m1_planning.InternalAgentClient = MockClient

    final_state = m1_planning_node(dummy_state)
    
    print("\n--- Execution Summaries ---")
    for es in final_state.get("execution_summaries", []):
        print(f"Agent: {es.agentIdentity}, Status: {es.status}, Outcome: {es.finalOutcome}")
        if es.errors:
            print(f"Errors: {es.errors}")
            
    print("\n--- Plan Generated ---")
    plan = final_state.get("plan")
    if plan:
        print(json.dumps(plan, indent=2))
    else:
        print("No plan generated.")

if __name__ == "__main__":
    run_smoke_test()
