"""
tests/test_m4_validation.py — Offline unit tests for the M4 validation agent.

All HTTP calls and Gemini calls are mocked so no network is required.
"""

import json
import pytest
from unittest.mock import patch, MagicMock
from state.workflow_state import WorkflowState
from agents.m4_validation import m4_validation_node


# ── Helpers ──────────────────────────────────────────────────────────────────

def make_state(
    upstream_failed=False,
    has_hotels=True,
    has_vehicle=True,
    has_plan=True,
) -> WorkflowState:
    """Build a minimal WorkflowState for M4 tests."""
    summaries = []
    if upstream_failed:
        from schemas.models import AgentExecutionSummary
        summaries.append(AgentExecutionSummary(
            agentIdentity="m1_planning",
            status="AgentFailed",
            finalOutcome="Fail",
        ))

    hotels = []
    if has_hotels:
        hotels = [{
            "OvernightAreaId": 1,
            "OvernightAreaName": "Kandy",
            "CheckInDate": "2026-10-01",
            "CheckOutDate": "2026-10-03",
            "NightCount": 2,
            "HotelId": 10,
            "HotelName": "Test Hotel",
            "RoomLines": [
                {"RoomId": 101, "RoomType": "Double", "Quantity": 2,
                 "CapacityPerRoom": 2, "PricePerNight": 5000.0, "LineCost": 20000.0}
            ],
            "SectionCapacity": 4,
            "SectionCost": 20000.0,
            "Explanation": "Cheapest option.",
        }]

    vehicles = []
    if has_vehicle:
        vehicles = [{
            "VehicleId": 201,
            "StartDate": "2026-10-01T00:00:00Z",
            "EndDate": "2026-10-03T00:00:00Z",
            "PickupLatitude": 6.9271,
            "PickupLongitude": 79.8612,
            "PickupNote": "Airport",
            "Cost": 10000.0,
            "Explanation": "Cheapest van.",
            "VehicleDetails": {"vehicleId": 201, "model": "KDH Van"},
        }]

    plan = {}
    if has_plan:
        plan = {
            "overnight_sections": [
                {
                    "overnight_area_id": 1,
                    "overnight_area_name": "Kandy",
                    "check_in_date": "2026-10-01",
                    "check_out_date": "2026-10-03",
                    "night_count": 2,
                }
            ],
            "daily_visits": [],
        }

    return WorkflowState({
        "proposal_id": "test-proposal-123",
        "trip_id": 1,
        "input_snapshot": {
            "StartDate": "2026-10-01T00:00:00Z",
            "EndDate": "2026-10-03T00:00:00Z",
            "GroupSize": 4,
            "Budget": 60000,
        },
        "execution_summaries": summaries,
        "hotels": hotels,
        "vehicles": vehicles,
        "plan": plan,
        "weather": [
            {"destination": "Kandy", "date": "2026-10-01",
             "status": "Available", "advisory": "Clear skies."}
        ],
        "accommodation_summary": {"cost": 20000.0, "remaining_budget": 40000.0},
        "transport_summary": {"transport_cost": 10000.0},
    })


def make_backend_result(is_valid=True, issues=None, issue_codes=None):
    return {
        "isValid": is_valid,
        "issueCodes": issue_codes or [],
        "issues": issues or [],
        "warnings": [],
        "accommodationCost": 20000.0,
        "transportCost": 10000.0,
        "totalCost": 30000.0,
        "budget": 60000.0,
        "currency": "LKR",
        "staleInputDetected": False,
    }


def make_gemini_explanation(is_valid=True):
    verdict = "PASSED" if is_valid else "FAILED"
    mock = MagicMock()
    mock.overall_summary = f"{verdict}: proposal is valid."
    mock.issue_explanations = []
    mock.warnings = []
    mock.traveler_message = "Your trip has been validated." if is_valid else "Issues found."
    return mock


# ── Tests ─────────────────────────────────────────────────────────────────────

class TestM4UpstreamCheck:
    def test_skips_when_upstream_failed(self):
        state = make_state(upstream_failed=True)
        new_state = m4_validation_node(state)

        m4_summary = next(
            (s for s in new_state["execution_summaries"]
             if s.agentIdentity == "m4_validation"), None
        )
        assert m4_summary is not None
        assert m4_summary.status == "ValidationSkipped"
        assert m4_summary.finalOutcome == "Fail"
        assert new_state["is_valid"] is False

    def test_skips_does_not_call_backend(self):
        state = make_state(upstream_failed=True)
        with patch("agents.m4_validation.InternalAgentClient") as MockClient:
            m4_validation_node(state)
            MockClient.return_value.validate_proposal.assert_not_called()


class TestM4BackendValidation:
    @patch("agents.m4_validation.InternalAgentClient")
    @patch("agents.m4_validation._gemini_explanation")
    def test_passes_when_backend_says_valid(self, mock_gemini_exp, MockClient):
        MockClient.return_value.validate_proposal.return_value = make_backend_result(is_valid=True)
        mock_gemini_exp.return_value = make_gemini_explanation(is_valid=True)

        state = make_state()
        new_state = m4_validation_node(state)

        assert new_state["is_valid"] is True
        m4_sum = next(s for s in new_state["execution_summaries"] if s.agentIdentity == "m4_validation")
        assert m4_sum.status == "Success"
        assert m4_sum.finalOutcome == "Pass"

    @patch("agents.m4_validation.InternalAgentClient")
    @patch("agents.m4_validation._gemini_explanation")
    def test_fails_when_backend_says_invalid(self, mock_gemini_exp, MockClient):
        MockClient.return_value.validate_proposal.return_value = make_backend_result(
            is_valid=False,
            issue_codes=["CAPACITY_INSUFFICIENT"],
            issues=["Hotel capacity 2 < group size 4."],
        )
        mock_gemini_exp.return_value = make_gemini_explanation(is_valid=False)

        state = make_state()
        new_state = m4_validation_node(state)

        assert new_state["is_valid"] is False
        m4_sum = next(s for s in new_state["execution_summaries"] if s.agentIdentity == "m4_validation")
        assert m4_sum.status == "Success"
        assert m4_sum.finalOutcome == "Fail"

        # The issue codes must be persisted in validationResults.
        val_results = json.loads(m4_sum.validationResults)
        assert "CAPACITY_INSUFFICIENT" in val_results["issueCodes"]

    @patch("agents.m4_validation.InternalAgentClient")
    def test_agent_fails_when_backend_call_raises(self, MockClient):
        MockClient.return_value.validate_proposal.side_effect = Exception("Connection refused")
        state = make_state()
        new_state = m4_validation_node(state)

        assert new_state["is_valid"] is False
        m4_sum = next(s for s in new_state["execution_summaries"] if s.agentIdentity == "m4_validation")
        assert m4_sum.status == "AgentFailed"
        assert "Connection refused" in m4_sum.errors


class TestM4GeminiCannotOverride:
    """Gemini is used for explanation only — it must not change Pass/Fail."""

    @patch("agents.m4_validation.InternalAgentClient")
    @patch("agents.m4_validation.get_gemini_client")
    def test_backend_valid_wins_even_if_gemini_says_failed(self, mock_get_llm, MockClient):
        MockClient.return_value.validate_proposal.return_value = make_backend_result(is_valid=True)

        # Gemini hallucination: says FAILED even though backend says valid.
        mock_llm = MagicMock()
        mock_llm.generate_structured.return_value = MagicMock(
            overall_summary="FAILED: something is wrong.",
            issue_explanations=["Made-up issue."],
            warnings=[],
            traveler_message="Hallucination.",
        )
        mock_get_llm.return_value = mock_llm

        state = make_state()
        new_state = m4_validation_node(state)

        # Backend wins — is_valid must be True.
        assert new_state["is_valid"] is True
        m4_sum = next(s for s in new_state["execution_summaries"] if s.agentIdentity == "m4_validation")
        assert m4_sum.finalOutcome == "Pass"

        # But a warning about the mismatch should be present.
        val = json.loads(m4_sum.validationResults)
        assert any("authoritative" in w.lower() or "INTERNAL" in w for w in val["warnings"])

    @patch("agents.m4_validation.InternalAgentClient")
    @patch("agents.m4_validation.get_gemini_client")
    def test_backend_invalid_wins_even_if_gemini_says_passed(self, mock_get_llm, MockClient):
        MockClient.return_value.validate_proposal.return_value = make_backend_result(
            is_valid=False,
            issue_codes=["OVER_BUDGET"],
            issues=["Total cost exceeds budget."],
        )

        # Gemini hallucination: says PASSED even though backend says invalid.
        mock_llm = MagicMock()
        mock_llm.generate_structured.return_value = MagicMock(
            overall_summary="PASSED: everything looks great.",
            issue_explanations=[],
            warnings=[],
            traveler_message="All good!",
        )
        mock_get_llm.return_value = mock_llm

        state = make_state()
        new_state = m4_validation_node(state)

        # Backend wins — is_valid must be False.
        assert new_state["is_valid"] is False
        m4_sum = next(s for s in new_state["execution_summaries"] if s.agentIdentity == "m4_validation")
        assert m4_sum.finalOutcome == "Fail"


class TestM4WeatherPolicy:
    """Weather issues must never cause M4 to fail."""

    @patch("agents.m4_validation.InternalAgentClient")
    @patch("agents.m4_validation._gemini_explanation")
    def test_weather_unavailable_does_not_fail(self, mock_gemini_exp, MockClient):
        MockClient.return_value.validate_proposal.return_value = make_backend_result(is_valid=True)
        mock_gemini_exp.return_value = make_gemini_explanation(is_valid=True)

        state = make_state()
        # Overwrite weather with all-unavailable entries.
        state["weather"] = [
            {"destination": "Kandy", "date": "2026-10-01",
             "status": "Unavailable_ProviderError", "advisory": "Service down."}
        ]

        new_state = m4_validation_node(state)
        assert new_state["is_valid"] is True
        m4_sum = next(s for s in new_state["execution_summaries"] if s.agentIdentity == "m4_validation")
        assert m4_sum.status == "Success"
        assert m4_sum.finalOutcome == "Pass"


class TestM4PayloadAssembly:
    """Verify helpers assemble the correct payload shapes."""

    @patch("agents.m4_validation.InternalAgentClient")
    @patch("agents.m4_validation._gemini_explanation")
    def test_hotels_assembled_correctly(self, mock_gemini_exp, MockClient):
        captured = {}

        def capture_validate(hotels, vehicle, overnight_sections):
            captured["hotels"] = hotels
            return make_backend_result(is_valid=True)

        MockClient.return_value.validate_proposal.side_effect = capture_validate
        mock_gemini_exp.return_value = make_gemini_explanation(is_valid=True)

        state = make_state()
        m4_validation_node(state)

        hotels = captured["hotels"]
        assert len(hotels) == 1  # one RoomLine from the fixture
        assert hotels[0]["roomId"] == 101
        assert hotels[0]["numberOfRooms"] == 2
        assert hotels[0]["checkInDate"] == "2026-10-01"

    @patch("agents.m4_validation.InternalAgentClient")
    @patch("agents.m4_validation._gemini_explanation")
    def test_vehicle_assembled_correctly(self, mock_gemini_exp, MockClient):
        captured = {}

        def capture_validate(hotels, vehicle, overnight_sections):
            captured["vehicle"] = vehicle
            return make_backend_result(is_valid=True)

        MockClient.return_value.validate_proposal.side_effect = capture_validate
        mock_gemini_exp.return_value = make_gemini_explanation(is_valid=True)

        state = make_state()
        m4_validation_node(state)

        v = captured["vehicle"]
        assert v["vehicleId"] == 201
        assert abs(v["pickupLatitude"] - 6.9271) < 0.0001

    @patch("agents.m4_validation.InternalAgentClient")
    @patch("agents.m4_validation._gemini_explanation")
    def test_no_vehicle_when_state_empty(self, mock_gemini_exp, MockClient):
        captured = {}

        def capture_validate(hotels, vehicle, overnight_sections):
            captured["vehicle"] = vehicle
            return make_backend_result(is_valid=False, issue_codes=["MISSING_VEHICLE"])

        MockClient.return_value.validate_proposal.side_effect = capture_validate
        mock_gemini_exp.return_value = make_gemini_explanation(is_valid=False)

        state = make_state(has_vehicle=False)
        m4_validation_node(state)

        assert captured["vehicle"] is None


class TestM4GeminiFailsGracefully:
    """If Gemini raises, M4 still records the backend verdict."""

    @patch("agents.m4_validation.InternalAgentClient")
    @patch("agents.m4_validation.get_gemini_client")
    def test_gemini_error_does_not_change_outcome(self, mock_get_llm, MockClient):
        MockClient.return_value.validate_proposal.return_value = make_backend_result(is_valid=True)

        mock_llm = MagicMock()
        mock_llm.generate_structured.side_effect = Exception("Quota exceeded")
        mock_get_llm.return_value = mock_llm

        state = make_state()
        new_state = m4_validation_node(state)

        assert new_state["is_valid"] is True
        m4_sum = next(s for s in new_state["execution_summaries"] if s.agentIdentity == "m4_validation")
        assert m4_sum.status == "Success"
        assert m4_sum.finalOutcome == "Pass"
