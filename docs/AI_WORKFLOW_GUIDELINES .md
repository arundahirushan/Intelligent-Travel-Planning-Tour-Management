# AI Workflow Guidelines

**Project:** Intelligent Travel Planning & Tour Management  

**Version:** 1.1  

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

This version uses the guide supplied by the project owner and the latest **Shared AI Foundation Implementation Report**, supplied on 2026-09-27. The repository was not inspected for this document update. Requirements below describe intended behavior; a report is evidence of what was claimed, not proof that every path works.

| Area | Latest reported state | Remaining uncertainty |
| --- | --- | --- |
| Shared checkout | Multiple hotel stays and an optional vehicle use a common 12-hour hold; `PlaceApprovedProposalHoldAsync` was reported present. | PostgreSQL transaction, locking, expiry and retry behavior need verification. |
| Proposal lifecycle | `WorkflowService`, persisted proposals and generation/review methods now exist according to the reports. The earlier “no persisted lifecycle” note is superseded. | Full route authorization, identity, stale-input and state-transition coverage were not supplied. |
| Failure recovery | `GenerateProposalAsync` reportedly records `GenerationFailed` after AI call errors and recovers records older than five minutes. | Timeout configuration, invalid JSON, request cancellation, late responses and restart recovery were not demonstrated. The stale threshold must exceed the allowed run time plus a margin. |
| Agentic hotel rules | `ValidateAgenticProposalAsync` reportedly runs in Accept and Approve. Option B checkout testing reportedly passes. | Separate proof for overnight vehicle-only/empty payloads, itinerary-area coverage and overnight-stop agreement was not supplied. |
| M4 gate | Accept reportedly requires explicit M4 `Pass` in execution summaries. | Approval must also enforce complete, current, valid results; its full gate was not demonstrated. |
| Manual booking | Existing manual callers use the shared hold service. | The latest report does not confirm fixing or testing the previously reported agentic-area restriction on manual bookings. Keep this regression open. |
| Internal scope | `InternalAgentController` reportedly resolves `X-AI-ProposalId`, requires a Generating proposal, and restricts hotel searches to trip areas. | Verify binding to the backend-issued run context, all routes, dates, permissions and both services' secret checks. A caller-selected ID alone is not authorization. |
| Python tools | `ai-service/tools/client.py` reportedly exists and sends the internal secret and proposal ID. | Typed response compatibility, actual node wiring, permission enforcement and client tests were not demonstrated. |
| Placeholder behavior | M1–M3 reportedly return `AgentNotImplemented`; M4 detects unfinished upstream agents; `main.py` maps this to generation failure. | Verify graph failure handling and retention of earlier node results. Actual agents remain unfinished. |
| Version uniqueness | A `[TripId, Version]` unique index and `FixTripProposalVersionConstraint` migration were reportedly generated. | Application to a database and conflict recovery were not evidenced. A unique index alone does not solve concurrent version allocation. This touched previously deferred scope; do not reverse it without reviewing the actual code. |
| Tests | Latest report says all 62 checkout tests pass. | No exact latest commands, full-suite totals, skip count or Python results were supplied. An earlier report listed two skipped checks; their current status is unconfirmed. |
| Weather, proposal screens, PayHere | Still deferred in the available reports. | Do not describe the complete booking feature as finished. |

**Development position:** Member 1 can begin focused M1 development after a narrow check of the actual input/output models, graph connection and tools needed by M1. This is not clearance for real booking execution. Do not restart a broad audit before every member; inspect and test the dependency that their component actually needs.

Known outstanding work:

- Confirm and correct the manual booking regression without imposing agentic coverage rules on manual bookings.
- Confirm Python execution, tool permissions, shared serialization and partial-output inspection as the agents use these interfaces.
- Resolve approval/checkout split commits and concurrent approval/rejection before relying on the real approval-to-hold flow.
- Finish concurrent proposal version allocation/retry handling; the reported unique index is only part of this work.
- Verify simultaneous proposal retries and last-room/vehicle races using real PostgreSQL.
- Inspect duplicate-error handling using the database unique-violation code and constraint, rather than exception message text.
- Verify current checkout status/expiry exposure, expiry release and the complete payment flow before final delivery.

Carry these limitations forward in each handoff until code and checks show they are resolved. Do not treat all outstanding items as prerequisites for writing M1.

## 3. Product flow

### 3.1 Trip inputs

A traveler creates a trip with its name, start date, end date, budget, group size, interests, and selected destination areas using the current trip flow.

Destinations represent areas, such as Kandy Area or Matara Area. Admin creates areas. A hotel belongs to an area chosen by its owner.

### 3.2 Manual booking

- Hotel and vehicle bookings remain separate traveler actions.

- Each action uses the existing shared 12-hour hold implementation.

- A combined manual cart is not required.

- The agentic selected-area restriction must not be imposed on manual hotel search/booking.
- Manual stays may cover only part of the trip, within existing manual date rules. Do not impose agentic whole-trip coverage or duplicate-area rules on separate manual bookings.

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

Two separate project-owner decisions apply together:

- **Area visits — Option B:** visit an area without necessarily booking a hotel there.
- **Accommodation coverage — Option A:** every overnight agentic trip needs hotels covering every night.

The option letters came from different questions. Option A does not cancel Option B. Personal accommodation and partially uncovered nights are not included in the current scope.

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

- A hotel-stay list and an optional vehicle. An incomplete working state may have no hotels; a completed overnight proposal must cover every night.

- Real hotel/room/vehicle identifiers, dates, and supported occupancy/pickup fields. CheckoutId is added by the backend only after a hold; agents must not invent one.

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

## 15. Scope and team ownership

### Shared foundation responsibilities

- Shared state/contracts and a canonical JSON fixture.

- Proposal persistence and execution summaries.

- Internal authentication and tool clients/endpoints.

- Separate agent modules and real graph skeleton.

- Generation/review APIs and state transitions.

- Admin approval-to-hold integration.

- Option B validation updates.

- Tests, configuration examples, startup instructions, and this documentation.

These are responsibilities, not a claim that every foundation requirement is verified. Use Section 2 for reported progress and unresolved work.

### Agent and later integration work

- Actual M1-M4 planning, recommendation, and validation-agent behavior.

- Real weather integration with M3.

- React traveler proposal screens.

- Flutter Admin proposal screens.

- PayHere and verified confirmation.

- Deferred PostgreSQL race tests and complete end-to-end verification.

### Coordinate shared files

Coordinate changes to contracts, graph wiring, main.py, workflow APIs/services, shared authentication/tool code, AppDbContext, and migrations. Agent owners should mainly change their own modules, permitted tool adapters, and focused tests.

Every member must be able to explain their agent's inputs, outputs, permissions, business rules, failure behavior, and tests.

## 16. Shared implementation plan: how the members connect

This section is a working plan, not a fixed implementation recipe. Keep the agreed product rules above stable. Members may refine helpers, prompts and internal code as they learn from the repository. Changes affecting another member's contract or a business decision must be discussed, documented and merged before dependent work continues.

### 16.1 Overall connection

The backend creates and owns one proposal attempt and provides its trip snapshot to Python. Python passes typed results through the graph. Each agent adds its own result; it does not replace the original trip requirements or invent another agent's result.

| Stage | Receives | Produces | Next consumer |
| --- | --- | --- | --- |
| Backend starts generation | Authorized trip and generation request | Proposal/run identity, version and trip planning context | M1 and the shared graph state |
| M1 plans | Trip dates, selected areas, travelers, budget, interests and available destination context | Ordered daily area visits, overnight sections and planning constraints | M2 and M3 |
| M2 selects accommodation | M1 overnight sections and scoped room search results | Real hotel/room selections, stay dates, supported occupancy, costs and limitations | Assembly and M4 |
| M3 selects transport and checks conditions | Trip context, M1 route, relevant M2 results and permitted tools | Optional vehicle, dates/capacity/costs, weather information or explicit unavailability, advisories | Assembly and M4 |
| Assembly combines results | M1 plan, M2 stays and M3 transport/advisories | One consistent candidate proposal, with itemized priced costs and unresolved issues | M4 |
| M4 validates | Trip snapshot, assembled candidate, upstream results and allowed verification tools | Pass, Fail or NeedsRevision, with structured reasons | Backend persistence and review gates |
| Backend handles decisions | Saved current proposal and human decisions | Traveler review, Admin review, then a shared hold or valid one-day no-booking approval | Later traveler/Admin screens and payment |

The initial runtime order remains **M1 plan → M2 → M3 → assembly → M4**. This is distinct from the branch/merge order below. No parallel graph or automatic replanning loop is required for the first version.

Assembly belongs to M1's coordination responsibility, but may run as a small helper after M3. It combines typed outputs and performs arithmetic; it does not require an extra model call just to copy fields. It must never invent missing hotel, transport, weather or validation results.

### 16.2 Sequential implementation and merge order

The team has chosen **M1 → merge → M2 → merge → M3 → merge → M4 → merge**. Each member starts from the latest merged shared branch, so their code includes the previous member's work.

| Member | Main work | Required handoff before the next member starts |
| --- | --- | --- |
| Member 1 — M1 | Implement structured trip planning and its graph connection; establish the small shared model-call entry point once a provider is chosen; define how assembly consumes the existing agent contracts. | A real sample plan, stable overnight-section structure, tool/model entry points, focused checks and a documented way to inspect M1 output with later agents unfinished. |
| Member 2 — M2 | Use M1's overnight sections to search, assess and choose real accommodation. Return clear reasons when no suitable room is available. | A real M1→M2 example, room/date/occupancy/cost fields compatible with checkout, no-hotel handling for one-day trips, focused checks and unresolved limitations. |
| Member 3 — M3 | Use the plan to assess transport needs, search suitable vehicles, integrate the agreed weather source and explain travel conditions. | M1→M2→M3 example, supported pickup/date/capacity fields, weather coverage/limitations, focused checks and transport decision behavior. |
| Member 4 — M4 | Validate the combined output, wire any remaining assembly/validation integration, and produce clear findings. | End-to-end agent generation evidence, explicit validation outcomes, invalid-case checks and a list of backend/integration gaps still open. |

M1 must define the assembly interface without filling unfinished agents with fake success. M2 and M3 supply the real results later; M4 verifies the combined behavior. If existing shared contracts cannot carry a required result, change both sides and their fixture in that member's branch before merging.

### 16.3 What each member should do

1. Pull the latest merged branch. Read this guide, coding guidelines, the preceding member's handoff and the actual affected code.
2. Check the narrow dependencies needed for the component. If an API shape or status is uncertain, report what the files actually contain before relying on it. Do not infer working integration from a class name or implementation summary.
3. Implement the assigned component using simple code and existing services. Keep unrelated redesigns out of the change.
4. Test its success and meaningful failure cases. Verify that its output is usable by the next stage and that incomplete runs cannot pass review.
5. Update the relevant contract documentation, example fixture and this guide's progress/handoff notes. Record actual commands, results and limitations.
6. Review and merge the branch. The next member pulls that merge before beginning their implementation.

A handoff should identify the commit/branch, changed shared files, actual entry points, sample input/output, commands/results, required configuration and unresolved decisions. Do not include secrets. A short concrete handoff is sufficient; another large report is not required.

### 16.4 Minimal shared information

This is a plain-language description of the data that must connect. It does not prescribe new DTO names or override existing fields.

| Shared information | Why later members need it |
| --- | --- |
| Backend-issued identity, trip version/snapshot and schema version | Keeps every result attached to the intended attempt and original trip. |
| Dates, travelers, selected area IDs/names, budget and interests | Defines the requirements that agents must preserve. |
| Ordered visits with dates and area IDs | Allows multiple areas per day and proves selected-area coverage. |
| Overnight area with check-in/check-out dates | Lets M2 choose hotels covering the exact nights M1 planned. |
| Real room and vehicle IDs, supported occupancy/pickup fields | Allows the backend to revalidate and later book the saved selection. |
| Itemized prices, currency and unpriced items | Lets assembly and M4 compare like-for-like costs without pretending the trip total is all-inclusive. |
| Per-agent outcome, advisories, missing inputs and concise execution summary | Lets the next agent and the user understand what succeeded and what remains unresolved. |

Preserve backend identity and original trip inputs throughout. Use the actual shared fixture to establish JSON field names and date/status formats. Change C# and Python together when their exchanged contract changes. Do not silently add unsupported room quantities or fabricate pickup details.

### 16.5 Agent behavior and boundaries

**M1:** Include all selected areas, allocate every hotel night, and explain a reasonable visit order using supported information. It plans overnight areas; M2 chooses actual inventory. If the trip cannot be planned using the supplied constraints, return a clear issue instead of silently dropping an area or changing dates.

**M2:** Select real rooms for M1's overnight sections. Check supported capacity, dates, area and prices through tools. An empty hotel result is valid for a one-day trip. For an overnight trip, unavailable accommodation is a failure or revision need, never a successful empty list. Do not silently move an overnight stop to another area; return the constraint for coordinated revision.

**M3:** Assess vehicle suitability and travel conditions from supported data. It may use M2's actual hotel locations and priced subtotal where those fields are available. “No vehicle needed” is different from “no suitable vehicle found” and from a failed search. Missing mandatory transport inputs must be visible. Weather outside the provider's supported coverage, or an unavailable weather tool, must be reported honestly; do not turn general seasonal advice into a dated forecast.

**M4:** Combine deterministic checks with clear reasoning about feasibility, suitability and warnings. Never let an LLM explanation override failed date, coverage, capacity, identity, availability or price checks. Distinguish a valid one-day empty booking list from missing upstream results. Validate the priced portion of the budget and state which costs are not included.

Agents may explain their decisions and rank options. They cannot grant human approval, create reservations or mark payment confirmed. Each member must be able to explain the agent's distinct behavior; a renamed generic function is not enough.

### 16.6 Work while downstream agents are unfinished

After M1 is complete, a full run may still end in `GenerationFailed` because M2 is unfinished. That does not by itself mean M1 failed.

- Test the completed agent independently using the real typed contract and injected tool/model responses where appropriate.
- Keep completed outputs and execution summaries inspectable when a later node is unavailable. The exact storage/read path must be verified and documented in the handoff.
- Stop normal execution when a required agent fails or is unimplemented. A final error/diagnostic step may record the reason, but later agents must not appear successful.
- Use successful fake nodes only in isolated tests. Do not add an approvable production mock mode.
- A partial M1/M2/M3 result is not a completed traveler proposal and cannot be accepted or approved.

If the current graph or persistence discards M1 output, Member 1 should make the smallest necessary correction as part of connecting M1. This dependency is more useful to resolve than reopening unrelated checkout work.

### 16.7 Revision and failure approach

For the first version, use a bounded sequential run. On missing required input, unavailable inventory or invalid output, return structured reasons and preserve completed work. The backend maps these outcomes to the actual supported lifecycle states.

M4 `NeedsRevision` is not Pass. The traveler can later request deliberate regeneration through the existing backend flow. Do not add recursive M1→M4 retry loops or silently change the saved trip to force success. If automatic replanning is proposed later, first agree its limits and who may change each field.

Transient tool/model errors may use small bounded retries consistent with the shared timeout. Keep the total permitted run time aligned with stale-generation recovery so a live run is not declared abandoned.

### 16.8 Shared ownership and change control

| Shared area | Coordination rule |
| --- | --- |
| C# DTOs, Python schemas/state and shared fixture | The member needing a change updates both sides, verifies compatibility and explains the impact before the next merge. |
| Graph, `main.py` and assembly | M1 establishes the connection pattern; later members replace their own nodes and coordinate changes affecting earlier nodes. |
| Shared AI client and configuration | Set up once with M1 after provider selection; reuse it across agents. Do not copy provider-specific SDK code into every agent. |
| Scoped tools and permissions | Extend only what the current component needs, preserving backend scope and read-only access. |
| Workflow service, checkout and migrations | Change only for a demonstrated dependency or a separately agreed integration task. Preserve manual booking behavior. |

Keep confirmed business rules separate from implementation choices. A helper or prompt can be improved without changing the user's flow. Changing hotel coverage, selected areas, approval order, budget meaning or missing-input behavior needs an explicit decision. Record the reason, affected members and any contract changes in this guide's change log.

### 16.9 Decisions still open

These were discussed but not confirmed in the supplied conversation. This document does not silently choose them.

| Decision | When to settle it | Simple options |
| --- | --- | --- |
| Initial AI provider/model | Confirmed for M1 | Gemini 3.5 Flash-Lite (via google-genai SDK). Configurable via GEMINI_MODEL. |
| Itinerary detail | Confirmed for M1 | Area-level plan (daily visits and overnight stops). No attraction lists or schedules. |
| Vehicle requirement | Before M3 selection logic | Use an existing explicit trip preference if available; otherwise ask whether the vehicle is required, optional or conditional. Do not infer an answer from a missing database field. |
| Weather provider and missing-weather policy | Before M3/M4 weather integration | Agree the data source and when unavailability is a warning versus a revision blocker. No fabricated forecasts. |
| Missing mandatory pickup or other input | When actual contracts show it is required | Reuse existing collected input, or ask the owner how it should be collected. Do not invent defaults. |

Budget units and currency, actual room occupancy support and tool response shapes should first be read from the code. Ask only if the real files do not resolve them. Do not redesign an already implemented and confirmed decision just because another approach is possible.

Provider choice does not change the domain contracts. Keep model selection and credentials in server-side configuration. Switching to another provider may require a small client adapter and checks; an environment variable alone does not guarantee compatibility. Do not build a large provider-switching framework before it is needed.

## 17. Integration after the four agents

1. Verify real M1→M2→M3→assembly→M4 generation with representative trips and controlled failure cases. No holds are created during these runs.
2. Fix and verify approval/checkout transaction consistency before relying on Admin approval with real inventory. Resolve relevant state-transition, manual-booking and current-checkout-status gaps.
3. Build React traveler review and Flutter Admin review against the saved proposal APIs. Keep acceptance and Admin approval separate.
4. Verify Admin approval → one shared 12-hour hold, including one-day vehicle-only and valid one-day no-booking cases, changed prices, unavailable inventory and repeated decisions.
5. Integrate PayHere and backend-verified payment confirmation.
6. Complete PostgreSQL concurrency/expiry checks and full end-to-end verification before final delivery.

Do not describe real approvals or bookings as fully ready merely because agent generation works. Deferred database risks must be closed before the final booking workflow is relied on.

## 18. Reporting and change log

After each implementation task, report changed files, actual interfaces, migrations, exact checks/results, remaining placeholders and unverified behavior. Distinguish code-inspected behavior from tests and live verification. This document update did not run application builds or tests.

| Version | Date | Change |
| --- | --- | --- |
| 1.0 | 2026-09-27 | Initial guideline based on the agreed foundation prompt. Records Option B day visits, four-agent boundaries, persisted review flow, Admin-only hold creation, payment deferral and verification limits. |
| 1.1 | 2026-09-27 | Adds the flexible shared implementation plan, sequential member/merge handoffs, data connections, assembly ownership, incomplete-agent testing, shared-file coordination and open decisions. Updates reported foundation progress without claiming independent verification. Clarifies Option A hotel coverage alongside Option B day visits, manual-booking boundaries, post-hold checkout identity and deferred approval consistency work. Restores normal Markdown formatting. |
| 1.2 | 2026-09-27 | Implements M1 Trip Planning & Coordination agent. Confirms Gemini 3.5 Flash-Lite and area-level planning. Types output plan. |
