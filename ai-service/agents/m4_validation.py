"""
agents/m4_validation.py — M4 Trip Validation & Safety Agent

Sequential flow
---------------
1. Upstream Check  — if M1/M2/M3 failed, set ValidationSkipped and exit.
2. Assemble        — build the ValidateProposalRequestDto from state.
3. Backend Validate — POST /api/internal/proposals/validate (authoritative).
4. Gemini Review   — generate a plain-language summary of issues/warnings.
                     Gemini CANNOT change Pass/Fail or invent new issues.
5. Persist Result  — write execution summary; set state["is_valid"].

Weather Policy
--------------
Weather is advisory only. Missing forecasts and provider errors must never
cause M4 to fail or alter selections.

Rules
-----
- M4 validates; it does NOT replan or replace M1/M2/M3 selections.
- M4 cannot pass a proposal that the backend flagged as invalid.
- M4 cannot fail a proposal that the backend flagged as valid.
"""

import json
from datetime import datetime
from pydantic import BaseModel, Field
from typing import List, Optional

from state.workflow_state import WorkflowState
from schemas.models import AgentExecutionSummary
from tools.client import InternalAgentClient
from agents.llm_client import get_gemini_client


# ── Gemini output schema ─────────────────────────────────────────────────────
# Gemini is ONLY allowed to produce a human-readable explanation.
# Pass/Fail is determined entirely by the backend's IsValid field.

class M4GeminiExplanation(BaseModel):
    overall_summary: str = Field(
        ...,
        description=(
            "One-sentence top-level verdict. "
            "Must begin with 'PASSED:' or 'FAILED:' matching the backend result."
        ),
    )
    issue_explanations: List[str] = Field(
        default_factory=list,
        description=(
            "Plain-language explanation for EACH issue code returned by the backend. "
            "Do NOT invent issues not present in the backend result."
        ),
    )
    warnings: List[str] = Field(
        default_factory=list,
        description=(
            "Rephrase the advisory warnings from the backend. "
            "Weather unavailability is advisory only and must not appear as an issue."
        ),
    )
    traveler_message: str = Field(
        ...,
        description=(
            "A short (2-3 sentence) traveler-facing message. "
            "If invalid, name the specific blocking issues without exposing internal IDs. "
            "If valid, briefly confirm what was checked."
        ),
    )


M4_SYSTEM_PROMPT = """You are M4, the Trip Validation & Safety agent.
Your ONLY job is to explain a validation result that has already been determined by an authoritative backend service.

STRICT RULES:
1. You MUST NOT change the pass/fail verdict supplied by the backend.
2. You MUST NOT invent issues or warnings not present in the backend result.
3. Weather service unavailability is ADVISORY ONLY — do not treat it as a failure.
4. Bad weather forecasts are ADVISORY ONLY — do not treat them as failures.
5. Keep explanations factual, concise, and traveler-friendly.
6. Refer to hotels/vehicles by their area or role, not by internal IDs.
"""


# ── Helpers ──────────────────────────────────────────────────────────────────

def _build_hotels_payload(state: WorkflowState) -> list:
    """Convert state['hotels'] (M2 AccommodationSelection list) into the
    ValidationHotelItemDto format expected by the backend."""
    hotels = state.get("hotels") or []
    items = []
    for acc in hotels:
        for line in acc.get("RoomLines", []):
            items.append({
                "roomId": line["RoomId"],
                "checkInDate": acc["CheckInDate"],
                "checkOutDate": acc["CheckOutDate"],
                "numberOfRooms": line["Quantity"],
            })
    return items


def _build_vehicle_payload(state: WorkflowState) -> dict | None:
    """Convert state['vehicles'][0] (M3 selection) into ValidationVehicleItemDto."""
    vehicles = state.get("vehicles") or []
    if not vehicles:
        return None
    v = vehicles[0]
    return {
        "vehicleId": v["VehicleId"],
        "startDate": v["StartDate"],
        "endDate": v["EndDate"],
        "pickupLatitude": v["PickupLatitude"],
        "pickupLongitude": v["PickupLongitude"],
        "pickupNote": v.get("PickupNote"),
    }


def _build_overnight_sections_payload(state: WorkflowState) -> list:
    """Convert state['plan']['overnight_sections'] into ValidationOvernightSectionDto."""
    plan = state.get("plan") or {}
    sections = plan.get("overnight_sections") or []
    return [
        {
            "overnightAreaId": sec["overnight_area_id"],
            "checkInDate": sec["check_in_date"],
            "checkOutDate": sec["check_out_date"],
        }
        for sec in sections
    ]


def _upstream_failed(state: WorkflowState) -> bool:
    """Return True if any upstream agent recorded AgentFailed."""
    summaries = state.get("execution_summaries") or []
    return any(s.status == "AgentFailed" for s in summaries)


def _gemini_explanation(
    backend: dict,
    is_valid: bool,
    weather: list,
) -> M4GeminiExplanation:
    """Ask Gemini to produce a plain-language explanation of the backend result.
    
    The backend dict is the authoritative source; Gemini cannot alter it.
    Falls back to a minimal explanation if the Gemini call fails.
    """
    verdict = "PASSED" if is_valid else "FAILED"
    issue_codes = backend.get("issueCodes", [])
    issues = backend.get("issues", [])
    warnings = backend.get("warnings", [])

    # Build a concise weather advisory note (advisory only, never an issue).
    weather_notes = [
        f"Weather for {w.get('destination', '?')} on {w.get('date', '?')}: "
        f"{w.get('advisory', w.get('status', 'Unavailable'))}"
        for w in (weather or [])
    ]

    prompt = f"""{M4_SYSTEM_PROMPT}

Backend validation verdict: {verdict}
Issue codes: {json.dumps(issue_codes)}
Issues (human-readable): {json.dumps(issues)}
Advisory warnings from backend: {json.dumps(warnings)}
Weather advisory (NEVER a blocking issue): {json.dumps(weather_notes)}
Accommodation cost: {backend.get('accommodationCost', 0)} LKR
Transport cost: {backend.get('transportCost', 0)} LKR
Total cost: {backend.get('totalCost', 0)} LKR
Budget: {backend.get('budget', 0)} LKR
Stale inputs detected: {backend.get('staleInputDetected', False)}

Generate the explanation JSON exactly matching the schema.
overall_summary MUST begin with '{verdict}:'.
"""

    llm = get_gemini_client()
    try:
        return llm.generate_structured(prompt, M4GeminiExplanation)
    except Exception as e:
        # Gemini failure must NEVER change the pass/fail outcome.
        fallback_summary = (
            f"{verdict}: proposal {'passes' if is_valid else 'fails'} all checks."
            f" (Explanation service unavailable: {e})"
        )
        return M4GeminiExplanation(
            overall_summary=fallback_summary,
            issue_explanations=issues,
            warnings=warnings,
            traveler_message=(
                "Your trip proposal has been validated."
                if is_valid
                else f"The proposal could not be confirmed. Issues: {'; '.join(issues[:3])}"
            ),
        )


# ── Main node ────────────────────────────────────────────────────────────────

def m4_validation_node(state: WorkflowState) -> WorkflowState:
    """LangGraph node for M4."""
    if "execution_summaries" not in state:
        state["execution_summaries"] = []

    start_time = datetime.utcnow()

    # ── 1. Upstream Check ────────────────────────────────────────────────────
    if _upstream_failed(state):
        # One or more of M1/M2/M3 failed — validation cannot run over partial data.
        state["is_valid"] = False
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m4_validation",
            status="ValidationSkipped",
            finalOutcome="Fail",
            resultSummary=json.dumps({
                "message": "Validation skipped because an upstream agent failed.",
                "skipped": True,
            }),
            errors=json.dumps({"reason": "Upstream agent failure"}),
            startedAt=start_time,
            completedAt=datetime.utcnow(),
        ))
        return state

    # ── 2. Assemble request payload ──────────────────────────────────────────
    try:
        hotels_payload    = _build_hotels_payload(state)
        vehicle_payload   = _build_vehicle_payload(state)
        sections_payload  = _build_overnight_sections_payload(state)
    except Exception as e:
        state["is_valid"] = False
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m4_validation",
            status="AgentFailed",
            finalOutcome="Fail",
            errors=json.dumps({"error": f"Failed to assemble validation request: {e}"}),
            startedAt=start_time,
            completedAt=datetime.utcnow(),
        ))
        return state

    # ── 3. Backend Validate ──────────────────────────────────────────────────
    try:
        client = InternalAgentClient(proposal_id=state.get("proposal_id"))
        backend_result = client.validate_proposal(
            hotels=hotels_payload,
            vehicle=vehicle_payload,
            overnight_sections=sections_payload,
        )
    except Exception as e:
        # Network / HTTP error talking to the backend.
        # Cannot determine validity — treat as agent failure.
        state["is_valid"] = False
        state["execution_summaries"].append(AgentExecutionSummary(
            agentIdentity="m4_validation",
            status="AgentFailed",
            finalOutcome="Fail",
            errors=json.dumps({"error": f"Backend validation call failed: {e}"}),
            startedAt=start_time,
            completedAt=datetime.utcnow(),
        ))
        return state

    # Authoritative result from the backend — Gemini cannot change this.
    is_valid    = bool(backend_result.get("isValid", False))
    issue_codes = backend_result.get("issueCodes", [])
    issues      = backend_result.get("issues", [])
    warnings    = backend_result.get("warnings", [])
    accom_cost  = backend_result.get("accommodationCost", 0)
    trans_cost  = backend_result.get("transportCost", 0)
    total_cost  = backend_result.get("totalCost", 0)
    budget      = backend_result.get("budget", 0)
    stale       = backend_result.get("staleInputDetected", False)

    # ── 4. Gemini Review ─────────────────────────────────────────────────────
    weather = state.get("weather") or []
    explanation = _gemini_explanation(backend_result, is_valid, weather)

    # Safety assertion: Gemini must not contradict the backend.
    # If the Gemini-generated summary disagrees with is_valid, we log a warning
    # but keep the backend's authoritative verdict.
    gemini_says_passed = explanation.overall_summary.upper().startswith("PASSED")
    if is_valid != gemini_says_passed:
        warnings.append(
            "INTERNAL: Gemini summary verdict did not match backend result. "
            "Backend verdict is authoritative."
        )

    # ── 5. Persist Result ────────────────────────────────────────────────────
    state["is_valid"] = is_valid

    validation_results = {
        "isValid": is_valid,
        "issueCodes": issue_codes,
        "issues": issues,
        "warnings": warnings,
        "staleInputDetected": stale,
        "costBreakdown": {
            "accommodationCost": accom_cost,
            "transportCost": trans_cost,
            "totalCost": total_cost,
            "budget": budget,
            "currency": "LKR",
        },
    }

    result_summary = {
        "overallSummary": explanation.overall_summary,
        "issueExplanations": explanation.issue_explanations,
        "warnings": explanation.warnings,
        "travelerMessage": explanation.traveler_message,
    }

    state["execution_summaries"].append(AgentExecutionSummary(
        agentIdentity="m4_validation",
        status="Success",
        finalOutcome="Pass" if is_valid else "Fail",
        resultSummary=json.dumps(result_summary),
        validationResults=json.dumps(validation_results),
        startedAt=start_time,
        completedAt=datetime.utcnow(),
    ))

    return state
