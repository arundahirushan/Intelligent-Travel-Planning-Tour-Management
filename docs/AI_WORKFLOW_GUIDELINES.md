# AI Workflow Guidelines

**Project:** Intelligent Travel Planning & Tour Management  
**Version:** 1.0  
**Last updated:** 2026-09-27  
**Recommended repository location:** `docs/AI_WORKFLOW_GUIDELINES.md`  
**Purpose:** Persistent reference for Antigravity and the M1-M4 team when building or changing the AI workflow.

> This document records agreed requirements and the foundation implementation scope. It is not proof that the code implements them. Check the actual repository before making changes or claiming a feature works.

## 1. How to use and maintain this document

Before working on the AI system:

1. Read this file and `docs/CODING_GUIDELINES.md`.
2. Read relevant architecture documentation and the actual affected code, DTOs, controllers, services, migrations, and tests.
3. For UI work, also read the existing frontend guidelines and follow the current shared components.
4. Verify exact routes, field names, status names, and service signatures. Do not invent them from this document.
5. Keep code simple, readable, and consistent with the project. Reuse working services rather than duplicating business rules.
6. Ask the project owner if a business rule is genuinely ambiguous or a required input is unsupported. Give concrete options; do not guess silently.

The latest explicit project-owner decision takes precedence over this file. Update this file when such a decision changes the requirements. Older reports may describe superseded agent roles or booking rules; do not restore those rules accidentally.

When updating this document:

- Keep this filename stable, increment the version, and update the date and change log.
- Record requirement changes separately from implementation progress.
- Mark features as implemented only with actual code references and verification evidence.
- Record failed/skipped tests and environment limitations honestly.
- Preserve outstanding work until it is resolved; do not delete it to make the project appear complete.
- Do not include passwords, API keys, tokens, shared secrets, or private tool output.

## 2. Current evidence and boundaries

The following is based on Antigravity reports supplied on 2026-09-27, not an independent review of the repository:

| Area | Last reported state |
| --- | --- |
| Manual booking | Separate hotel and vehicle creation delegate to a common hold service. |
| Shared checkout | Supports multiple hotel stays and an optional vehicle under one checkout. |
| Agentic hold method | `PlaceApprovedProposalHoldAsync` exists; real signatures must be checked. |
| Proposal retries | ProposalId uniqueness, equivalence checking, post-lock recheck, and duplicate-conflict handling were reported implemented. |
| Checkout tests | Last reported: 62 passed, 2 skipped; EF InMemory does not verify PostgreSQL locking or transaction behavior. |
| Python AI service | Health endpoint and placeholders; actual agents not implemented at the last audit. |
| Workflow backend | Workflow controller and AI client were placeholders; no persisted proposal lifecycle at the last audit. |
| Weather, review screens, PayHere | Not implemented at the last audit. |
| Option B hotel validation | Required change; implementation has not yet been reported. |

Known deferred work:

- Verify simultaneous proposal retries and last-room/vehicle races using real PostgreSQL.
- Inspect duplicate-error handling; match the database unique-violation code and specific constraint rather than relying on exception message text.
- Verify expiry release and the complete payment flow before final delivery. Expiry behavior has been assumed working for current planning, not independently proven here.

The foundation can proceed while those existing inventory/retry checks are deferred. New workflow decisions still require consistent state transitions.

## 3. Product flow

### 3.1 Trip inputs

A traveler creates a trip with its name, start date, end date, budget, group size, interests, and selected destination areas using the current trip flow.

Destinations represent areas, such as Kandy Area or Matara Area. Admin creates areas. A hotel belongs to an area chosen by its owner.

### 3.2 Manual booking

- Hotel and vehicle bookings remain separate traveler actions.
- Each action uses the existing shared 12-hour hold implementation.
- A combined manual cart is not required.
- The agentic selected-area restriction must not be imposed on manual hotel search/booking.
- Supplies remain in the separate manual flow.
- Backend-verified payment will confirm a valid hold after PayHere is implemented. Unpaid expired holds release availability.

### 3.3 Agentic booking

1. Traveler requests a proposal for their own trip.
2. M1-M4 plan, search, assemble, and validate without creating holds.
3. ASP.NET Core saves the proposal and execution outcome.
4. Traveler can Accept, Reject, or Regenerate.
5. Accept sends that saved proposal version for Admin review; it does not reserve inventory.
6. Admin can Approve or Reject.
7. Approval revalidates the saved selection and uses the shared hold service.
8. All hotel stays and the optional vehicle receive one shared 12-hour hold, or none are held.
9. If a valid one-day proposal contains no hotel or vehicle, record approval without creating checkout.
10. PayHere will later confirm valid holds after backend verification.

**No holds during planning, traveler review, or pending Admin review. Admin approval is not payment confirmation.**

Supplies are excluded from agentic proposals. The traveler may book them manually later.

## 4. Destination visits and hotel nights: Option B

The traveler explicitly selected **Option B: visit an area without necessarily booking a hotel there**.

### Required rules

- Every selected destination area must appear in the proposed itinerary.
- Never silently drop a selected area.
- An area may be a day visit with zero hotel stays.
- Every hotel must belong to one of the trip's selected areas.
- An overnight trip must include hotel stays covering all required nights.
- First hotel check-in equals `Trip.StartDate`.
- Each subsequent check-in equals the previous checkout.
- Final hotel checkout equals `Trip.EndDate`.
- Every stay has positive duration; no date gaps or overlaps are allowed.
- One hotel may cover several consecutive nights.
- Keep the current restriction against separate repeated stays in the same destination area for this version.
- A one-day trip (`StartDate == EndDate`) has no hotel nights and must have an empty hotel list.
- A one-day trip may include a vehicle.
- Overnight stops in the itinerary must agree with the proposed hotel stays.

### Date convention

`EndDate` is the final travel day and final hotel checkout date. Jan 1-Jan 5 has four hotel nights, not five.

### Example

| Day | Area visits | Hotel |
| --- | --- | --- |
| 1 | Area A | Area A hotel |
| 2 | Area B day visit, then Area C | Area C hotel |
| 3 | Area C, then trip ends | Checkout; no further night |

This covers three areas and two hotel nights. It demonstrates the date/coverage rule, not proof of real-world travel feasibility.

The old rule requiring one hotel for every selected area must be removed from agentic validation. More areas than nights alone is not a rejection reason. Do not equate one area with one day or one hotel.

## 5. Architecture and authority

| Component | Responsibility |
| --- | --- |
| React | Traveler proposal interaction, implemented in a later stage. |
| Flutter | Admin review interaction, implemented in a later stage. |
| ASP.NET Core | Public API, identity, authorization, persistence, business rules, proposal decisions, and hold creation. |
| PostgreSQL | Durable proposals, workflow state, decisions, bookings, and execution summaries. |
| Python/FastAPI/LangGraph | Internal planning, controlled tool use, agent orchestration, and structured results. |

Both clients communicate only with ASP.NET Core. Python does not access PostgreSQL directly and cannot create or confirm bookings.

ASP.NET Core generates proposal IDs, associates proposals with their owners/trips, and controls approval status. Python cannot choose another owner, substitute proposal identity, or grant approval.

## 6. Four agent contracts

| Agent | Responsibility | Allowed capabilities | Required output |
| --- | --- | --- | --- |
| M1: Trip Planning & Coordination | Structure the trip, allocate area visits and overnight stops, delegate work, assemble results. | Read trip/destination planning context. | Structured plan, itinerary, requests to other agents, assembled proposal. |
| M2: Accommodation Analysis | Search and rank real available accommodation for overnight sections. | Read-only hotel/room searches and checks. | Valid hotel stays, price estimates, ranked choices/reasons, or clear failure. |
| M3: Transport & Travel Conditions | Find suitable available transport and obtain weather advice. | Read-only vehicle checks and the future weather tool. | Optional vehicle, prices, advisories, missing information, or clear failure. |
| M4: Trip Validation & Safety | Validate the assembled proposal and explain problems. | Relevant read-only checks and deterministic validators. | Pass, Fail, or NeedsRevision with structured reasons. |

Each role needs distinct typed inputs/outputs, enforced tool permissions, and visible execution results. Renaming the same behavior does not create distinct agents.

A simple sequential graph is sufficient initially:

`M1 plan -> M2 accommodation -> M3 transport/weather -> assembly -> M4 validation`

Assembly can be a helper owned by M1. It must not invent missing results. Bound execution and retries; avoid unlimited regeneration loops.

### Foundation placeholders

- Graph nodes and contracts must be real and importable.
- Actual agent reasoning will be implemented afterward.
- Unimplemented nodes return controlled unavailable/not-implemented failures.
- Missing M4 validation never counts as Pass.
- Test-only injected nodes may demonstrate orchestration; they must not become a production fake-success mode.
- Do not replace the four agents with a function that selects the first available hotel and vehicle.

## 7. Shared proposal and workflow state

Maintain matching C# DTOs and Python Pydantic models with a canonical JSON fixture. Separate incomplete working state from a completed proposal.

The contract must support:

- Schema version, backend-generated ProposalId, TripId, and proposal version.
- Snapshot of relevant trip inputs used for generation.
- Objective, structured plan, completed steps, and progress.
- Ordered day-by-day visits, including multiple areas on one day.
- Day visits and overnight stops.
- Zero or more hotel stays and an optional vehicle.
- Real checkout identifiers, dates, and supported occupancy/pickup fields.
- Itemized estimated prices, totals, and currency.
- Results from M1-M4, tool-result summaries, validation outcomes, warnings, and errors.
- Execution timestamps and outcome.

Inspect the actual checkout contract before defining booking fields. Do not invent room-quantity support or pickup coordinates. Missing mandatory inputs must produce a structured NeedsInput result.

Use the current room/vehicle billing conventions, explicit date formats, UTC event/expiry timestamps, and suitable decimal/money handling. Hotel stay dates are calendar dates; do not shift them accidentally through timezone conversion.

Distinguish priced hotel/vehicle costs from unpriced costs. A booking-component estimate must not be described as an all-inclusive trip cost.

## 8. Persistence and lifecycle

Use a simple proposal entity with normal columns for identity, relationship, version, status, and timestamps. Structured payloads may use PostgreSQL jsonb where supported by the current project setup.

Persist:

- Unique ProposalId and a version unique within its trip.
- Generation request identifier for retry handling.
- Trip input snapshot and structured result/state.
- Traveler/Admin decisions, actors, reasons, and times.
- Checkout reference after successful hold creation.
- Failure/revision information.
- Concise execution summaries: agent, tools, sanitized inputs/results, timings, validations, errors, and retries.

Do not store hidden chain-of-thought, credentials, or unnecessary raw prompts. Follow existing relationship/delete conventions and preserve current data during migrations.

### Required state meanings

Names may follow project conventions, but these meanings must exist:

| State | Meaning |
| --- | --- |
| Generating | A persisted generation attempt is in progress. |
| Generated | Generation completed; traveler review is possible. Acceptance still requires validation Pass. |
| GenerationFailed | Execution could not complete, including unimplemented agents or service failure. |
| NeedsInput / NeedsRevision | More information or a corrected proposal is required. |
| PendingAdminApproval | Traveler accepted this exact saved version. |
| TravelerRejected | Traveler rejected the proposal. |
| Superseded | A newer version replaces it; it is ineligible for approval. |
| AdminRejected | Admin rejected it with a reason. |
| HoldPlaced | Admin approval successfully produced a linked checkout. |
| ApprovedNoBookingRequired | Valid one-day proposal approved with no bookable items. |

Keep transition checks in one workflow service. Do not use TripStatus as the complete history of proposal versions, or mark a trip Confirmed because a hold was created.

## 9. Internal authentication and tools

Use a configured shared-secret header, such as `X-AI-Secret`, for intended internal ASP.NET Core/Python routes.

- Read secrets from environment/configuration/User Secrets; commit placeholders only.
- Reject absent, empty, or incorrect secrets and fail closed when required configuration is missing.
- Use appropriate constant-time comparison.
- Keep secrets out of clients, responses, logs, and tool summaries.
- Do not create fake users or grant general Admin access.
- Preserve public JWT/role authorization.
- Use the project's secure internal transport/HTTPS configuration when deployed.
- Keep health responses minimal.

Trace actual HTTP controllers. C# service interface methods alone are not endpoints.

Required read-only capabilities:

1. Planning context: trip inputs, selected areas, and existing itinerary ordering.
2. Hotel search: selected area, stay dates, availability, supported capacity, and prices.
3. Vehicle search: dates, capacity, availability, and prices.
4. Relevant verification checks for M4.

Reuse application services through thin internal endpoints. Resolve planning scope from a persisted proposal/run context; do not trust arbitrary owner or unrelated trip IDs in Python requests.

Validate inputs and return typed results. Register/enforce agent-specific tool permissions. Tools must not expose booking mutations, arbitrary SQL, shell commands, or arbitrary URLs.

Define the weather contract now. Until implemented, it returns unavailable/not-implemented, never a fabricated forecast.

## 10. Generation execution

Implement the existing AI client using HttpClientFactory, typed DTOs, configured service URL, and finite timeout. Handle network failure, invalid JSON, schema mismatch, and controlled Python failures without exposing secrets/internal exception details.

A straightforward awaited request is sufficient for the foundation:

1. Authorize traveler ownership and validate inputs.
2. Persist Generating and commit.
3. Call Python outside the database transaction.
4. Validate identity, schema, and returned state.
5. Persist the result or failure and available execution summaries.
6. Return a useful response.

Do not hold a database transaction during AI/network execution or use untracked fire-and-forget work. Define a bounded recovery behavior for interrupted/timed-out Generating records so they cannot block a trip forever.

Repeated generation requests with the same request identifier return the same attempt. Deliberate regeneration uses a new request identifier, ProposalId, and version. Prevent conflicting concurrent current versions with a small database-backed approach.

## 11. Review APIs and immutable decisions

### Traveler APIs

- Start generation for an owned trip.
- Read latest/specific proposal and execution summary for that trip.
- Accept, Reject, and Regenerate.

### Admin APIs

- List pending proposals.
- Read proposal and execution details.
- Approve or Reject with a reason, using the project's actual Admin/SuperAdmin roles.

### Invariants

- Enforce ownership on reads as well as writes.
- Acceptance requires a current completed proposal, valid business/schema checks, and M4 Pass.
- Accept and Reject create no holds.
- Regeneration makes the previous version ineligible for approval.
- Repeated identical decisions are handled consistently.
- Do not regenerate in a way that silently abandons an existing held checkout.
- Keep reviewed proposal content immutable.
- If relevant trip inputs change, the old proposal cannot be accepted/approved as though it reflects the new trip; compare input snapshots or use a reliable revision mechanism.
- Clients send decision data, not replacement hotel selections, totals, validation results, or approval flags.

## 12. Admin approval and checkout

Admin approval uses the exact saved, traveler-accepted proposal.

Before holding, check current status, identity, trip input snapshot, schema, dates, areas, coverage, capacity, identifiers, and arithmetic. Recheck prices and availability through the existing checkout path.

### With bookable items

- Map the saved selections to the actual checkout DTO.
- Pass the persisted ProposalId to `PlaceApprovedProposalHoldAsync`.
- Record CheckoutId and HoldPlaced only after the complete hold succeeds.
- Preserve all-or-nothing booking and the existing shared expiry.
- If current prices or selections no longer match what was reviewed, return a clear revision outcome instead of silently changing the accepted proposal.

### Without bookable items

For a valid one-day proposal with no hotel and no vehicle:

- Skip the hold service.
- Save ApprovedNoBookingRequired.
- Create no empty checkout, expiry timer, or payment obligation.

### Consistency

- Avoid a committed hold with no recorded workflow linkage, or HoldPlaced without a checkout.
- Reuse one transaction boundary for same-database approval/checkout writes where appropriate.
- Inspect existing CheckoutService transaction ownership; do not blindly nest BeginTransaction calls.
- Preserve existing manual callers.
- Repeated approval returns the same checkout and does not restart expiry.
- Concurrent approval/rejection must not produce contradictory decisions.
- Expose the linked checkout's current status/expiry when reading a HoldPlaced proposal. HoldPlaced records the earlier action; it is not proof that the reservation is still active.
- These APIs must not call ConfirmAsync or provide a payment bypass.

## 13. Deterministic validation

Shared structural checks must be ordinary deterministic code, reusable by M4 where appropriate and enforced by ASP.NET Core before high-impact actions.

Check schema, identities, selected-area visit coverage, room/hotel/area relationships, hotel night coverage, date boundaries, overnight-stop agreement, supported capacity, price arithmetic, and the defined priced portion of budget.

LLM assertions alone cannot establish validity, approval, payment, or availability.

Do not invent route durations or assume geographic feasibility. Relevant agents must assess feasibility using supported information and clearly report limitations.

## 14. Foundation acceptance tests

Add focused tests using the existing test style:

- Three visited areas and hotels in two selected areas, with all nights covered.
- Missing area visits, wrong hotel areas, night gaps, overlaps, invalid dates, and repeated area stays.
- One-day proposal with a vehicle, and one-day proposal with no bookable items.
- Ownership and actual API role authorization.
- Missing/incorrect internal secrets.
- Accept/Reject creating no holds.
- Regeneration identity/version and superseded-proposal rejection.
- Changed trip inputs blocking stale approval.
- Approval using saved selections rather than client replacements.
- Failed hold creation producing no false success.
- Repeated approval retaining checkout identity and expiry.
- Invalid/missing M4 validation blocking acceptance.
- Agent/network failures being saved honestly.
- Existing manual booking regression behavior.
- Shared JSON fixture serialized by Python and deserialized/validated by C#.
- Graph compilation and orchestration with injected test-only nodes.
- Production placeholders unable to produce fake success.

Use meaningful assertions. Do not globally suppress warnings or weaken tests just to obtain a green result.

Run backend build/tests, Python tests/import checks, FastAPI startup/health, protected-route checks, and migration inspection. Record exact commands, counts, and blockers.

Distinguish migrations generated, migrations tested on PostgreSQL, and migrations applied. Do not reset/delete data to make migrations pass. EF InMemory cannot prove PostgreSQL transactions, constraints, or advisory locks.

## 15. Foundation scope and team ownership

### Implement in the foundation

- Shared state/contracts and a canonical JSON fixture.
- Proposal persistence and execution summaries.
- Internal authentication and tool clients/endpoints.
- Separate agent modules and real graph skeleton.
- Generation/review APIs and state transitions.
- Admin approval-to-hold integration.
- Option B validation updates.
- Tests, configuration examples, startup instructions, and this documentation.

### Implement afterward

- Actual M1-M4 planning, recommendation, and validation-agent behavior.
- Real weather integration with M3.
- React traveler proposal screens.
- Flutter Admin proposal screens.
- PayHere and verified confirmation.
- Deferred PostgreSQL race tests and complete end-to-end verification.

### Coordinate shared files

Coordinate changes to contracts, graph wiring, main.py, workflow APIs/services, shared authentication/tool code, AppDbContext, and migrations. Agent owners should mainly change their own modules, permitted tool adapters, and focused tests.

Every member must be able to explain their agent's inputs, outputs, permissions, business rules, failure behavior, and tests.

## 16. Implementation reporting and next steps

After each implementation task, report changed files, routes/roles, migrations, exact checks/results, remaining placeholders, and unverified behavior. Do not claim the entire AI booking feature works while agents/screens/payment are unfinished.

Recommended order:

1. Build and review this shared foundation.
2. Implement M1 planning and delegation.
3. Implement M2 accommodation and M3 transport/weather against the stable contracts; these can be developed separately.
4. Implement M4's full validation behavior and integrate all agents.
5. Build React traveler review and Flutter Admin review.
6. Implement PayHere last.
7. Verify the full flow and deferred database cases before final delivery.

## 17. Change log

| Version | Date | Change |
| --- | --- | --- |
| 1.0 | 2026-09-27 | Initial guideline based on the agreed foundation prompt. Records Option B day visits, four-agent boundaries, persisted review flow, Admin-only hold creation, payment deferral, and verification limits. |

