# Flutter Development Guide

**Project:** Intelligent Travel Planner / TourManagement  
**Intended repository location:** `docs/FLUTTER_DEVELOPMENT_GUIDE.md`  
**Planning baseline:** 28 September 2026  
**Audience:** All four team members and coding assistants working on Flutter

## 1. Purpose and source of truth

Build one complete Android application for Admin and SuperAdmin, with four clearly owned feature areas. Use simple code that every member can understand, explain, test, and maintain.

This guide records the agreed Flutter scope. It is not proof that every listed backend feature has been tested live.

Before implementing a feature:

1. Read this guide, repository instructions, and `mobile/tour_management_mobile/INTEGRATION_GUIDE.md`.
2. Read the actual Flutter files, including authentication, API client, configuration, theme, navigation, and tests.
3. Read the relevant ASP.NET Core controller, service, request/response DTOs, validators, and authorization rules. Controller attributes alone may not describe all restrictions.
4. Check current Git status and the intended starting commit. Preserve existing work.
5. Compare the intended screen with the real API. Ask the project owner if a business rule, ownership decision, or required behavior is ambiguous. Do not guess silently.

Current code defines the available API contract. The team's latest confirmed decisions define intended behavior. If they disagree, explain the difference and agree on a focused correction; do not silently change either the requirements or backend.

Older reports may describe obsolete routes or unfinished scaffolding. The latest supplied backend audit is useful for locating code, but inspect the real files before copying routes, fields, roles, or statuses into Flutter.

## 2. Shared foundation and application boundaries

The shared foundation has been built. Login, closing/reopening the app with session restoration, logout, and login again were manually verified on an Android phone. Analysis, automated tests, and a debug APK build were reported as passing. This does not establish that all business features work.

The last reported foundation commit was `1e6cb30` on `feature/flutter-foundation`. Its merge/push state must be checked; this guide does not claim it has been merged into `development`.

Preserve and reuse:

- The existing login and session lifecycle.
- Secure token storage through `flutter_secure_storage`.
- The shared API client and bearer-token handling.
- Session clearing and return to login on protected-request HTTP 401 responses.
- Configurable API base URL through `API_BASE_URL`.
- The common Material theme and dashboard.

Application boundaries:

- Flutter is for **Admin and SuperAdmin**. M1–M4 are development responsibilities, not four new account roles.
- React remains the interface for Travelers, Hotel Owners, Transport Providers, and Suppliers.
- Flutter calls ASP.NET Core. It must not call Python, the database, or privileged third-party services directly.
- Backend authorization is the final authority. Hiding a button in Flutter is not an authorization mechanism.
- A working foundation does not mean the four placeholders are completed features.

## 3. Ownership and shared boundaries

| Member | Dashboard area | Owns |
| --- | --- | --- |
| M1 | Users, AI Proposals & Trips | User approvals and management, SuperAdmin account controls, AI proposal review, trip oversight |
| M2 | Hotels & Accommodation | Hotel moderation, property/room inspection, accommodation booking oversight |
| M3 | Vehicles & Destinations | Vehicle moderation, transport booking oversight, destination management |
| M4 | Supplier & Contracts | Supplier contract requests and contracts, supplier status, supply moderation, supply-order oversight |

Specific boundaries:

- M1 approves a Hotel Owner account; M2 approves that owner's hotel listing.
- M1 approves a Transport Provider account; M3 approves that provider's vehicle listing.
- M1 approves a Supplier account; M4 manages the supplier's contract and supply catalog oversight.
- M4 may display supplier identity information without duplicating user-account approval controls.
- M1 may display hotel, vehicle, and supply information within a trip/proposal without taking over those domains' management screens.
- M3 maintains destinations; other components reuse destination data.
- Flutter member ownership does not automatically rename or change the responsibilities of the Python AI agents.

All feature areas are available to Admin/SuperAdmin as permitted by the backend. SuperAdmin-only actions must remain restricted throughout navigation, screens, and API authorization.

## 4. M1 — Users, AI Proposals & Trips

### Build

| Area | Required behavior |
| --- | --- |
| Pending accounts | List pending provider registrations, inspect available account information, approve or reject |
| User management | List and filter users using supported search/role/status filters; display real account status |
| SuperAdmin controls | Create Admin accounts, promote eligible users, and delete eligible accounts using the actual backend restrictions and confirmation dialogs |
| Trips | List all traveler trips; open details with dates, destinations, itinerary, and available booking information |
| Trip intervention | Support the existing Admin force-cancel action with confirmation and the backend's required fields/rules |
| Pending AI proposals | List proposals that have reached the Admin review stage and open their details |
| Proposal review | Display the available plan, recommendations, cost information, validation results, and warnings; approve or reject an eligible proposal |

If there is no dedicated user-detail endpoint, use the information actually available in the authorized response. Do not invent an endpoint or make up missing fields.

### Preserve the AI approval process

The latest backend audit reports this sequence; verify it in current code before implementation:

1. A Traveler generates an AI proposal through the website.
2. The Traveler reviews it. Acceptance requires the workflow's validation result to pass and moves the proposal to `PendingAdminApproval`.
3. Admin/SuperAdmin reviews the pending proposal in Flutter.
4. Admin approval validates the proposal and can place inventory holds, producing `HoldPlaced` and a checkout reference. A proposal needing no bookings may become `ApprovedNoBookingRequired`.
5. Admin rejection produces `AdminRejected`.

**Admin approval is not proof of payment or final booking confirmation.** Display the returned state accurately. Do not mark a hold as a confirmed booking, collect payment in Flutter, or implement a separate booking engine.

Before displaying a hold countdown or expiry action, inspect the current checkout/hold implementation and its timestamps. Do not assume a duration or mark records expired locally. The audit does not fully establish payment, hold-expiry, or recovery behavior.

### Avoid

- Moving Traveler acceptance into the Admin app.
- Approving a proposal that has not reached the required stage.
- Adding new AI generation, editing, payment, or booking workflows without an agreed scope change.
- Editing hotel, vehicle, or supplier records from the proposal screen.
- Displaying missing prices or totals as zero, or missing validation results as “Passed.”

**Suggested build order:** Users → trips → proposal list/details → proposal actions.

## 5. M2 — Hotels & Accommodation

### Build

- Pending-hotel list and all-hotels list, with backend-supported filters and pagination.
- Hotel details showing available property information, owner, destination, status, rooms, and prices.
- Approve, reject, and suspend actions when allowed by the hotel's current status and backend rules.
- System-wide accommodation booking overview with supported filters, actual statuses, and available booking information.
- Detail navigation only where an authorized response supports it; a summary must not pretend to contain a complete booking record.

Ensure authenticated Admins can inspect pending and suspended hotels. Do not assume an endpoint described as “public” exposes every status without the authorization header.

### Avoid

- Rebuilding Hotel Owner hotel/room creation and editing workflows in Flutter.
- Approving Hotel Owner accounts here; that belongs to M1.
- Creating Traveler reservations or changing prices from Admin oversight screens.
- Adding booking cancellation or record deletion just because endpoints exist. The initial booking scope is read-only oversight; additional destructive actions need an explicit scope decision and verified lifecycle rules.

**Suggested build order:** Pending hotels → details/moderation → all hotels → booking oversight.

## 6. M3 — Vehicles & Destinations

### Build

- Pending-vehicle list and all-vehicles list with supported filters and pagination.
- Vehicle details showing available provider information, vehicle type, capacity, price, and status.
- Approve, reject, and suspend actions following backend status rules.
- System-wide vehicle booking overview with available dates, traveler/provider information, vehicle, and status.
- Destination list, details, create, edit, and delete using the actual DTOs and backend constraints.
- Clear feedback when a destination cannot be deleted because it is used elsewhere.

### Avoid

- Driver management, driver allocation, route scheduling, or activity/tour management. The agreed scope is vehicle rental with a driver included.
- Transport Provider account approval here; that belongs to M1.
- Rebuilding the provider's fleet-management website inside Flutter.
- Adding booking cancellation/deletion without an explicit scope decision. Initial booking oversight is read-only.
- Treating weather analysis as a new Flutter weather-management module. Relevant proposal weather information belongs in M1's review display when provided by the API.

**Suggested build order:** Vehicle review → destination management → booking oversight.

## 7. M4 — Supplier & Contracts

### Build

- Supplier overview using authorized supplier/user information and contract-status data.
- Contract-request list and details, including available supplier information and requested terms.
- Approve/reject contract requests using the backend's actual request format.
- Contract list, details, and status filters, including active and historical records.
- Contract termination with confirmation and any required reason.
- A duration dropdown with **1 year, 2 years, or 3 years** wherever an Admin is responsible for selecting contract duration. Show resulting dates for review; do not offer an unrestricted end-date picker as a substitute.
- Supply listings and details with supplier/status filters supported by the API.
- Supply removal through the dedicated moderation endpoint, collecting the required removal reason.
- Read-only supply-order oversight, showing the information and statuses returned by the backend.

### Contract rules and unresolved details

- One active, valid contract per supplier at a time.
- Another contract can be requested after expiry or termination, according to the agreed lifecycle.
- Prevent duplicate pending requests according to backend rules.
- Preserve historical contracts; termination is not deletion of history.
- The latest audit reports service checks for active contracts and pending requests. It does not demonstrate that concurrent approvals cannot bypass those checks; do not claim that guarantee without evidence.
- The audit reports that backend date validation accepts ranges beyond exactly 1/2/3 years. A Flutter dropdown improves input but cannot enforce the rule for every API caller. Record this as a backend validation gap and coordinate a focused fix before claiming the rule is fully enforced.
- The audit also mentions renewals extending an existing contract. Verify whether active-contract renewal is allowed and whether it matches the agreed lifecycle before exposing a renewal action. Ask if the code and intended rule conflict.
- The audit mentions direct Admin contract creation. Do not add a second contract-creation flow automatically; first confirm whether the application-approval flow is sufficient and where duration is selected in the actual DTOs.

Do not silently calculate or overwrite dates during approval if approval accepts no such fields. Resolve the contract input flow first. If Flutter must convert a selected duration to dates for an existing API, match the backend's date semantics and calendar-year rules.

### Avoid

- Supplier account approval or role changes; those belong to M1.
- Supplier-facing supply creation/editing, inventory maintenance, or order fulfillment workflows.
- Treating account approval as contract approval.
- Allowing a dropdown to be described as complete backend validation.
- Unrequested supply-order cancellation/deletion actions.

**Suggested build order:** Supplier/contract information → requests and approvals → contracts/termination → supply moderation → order oversight.

## 8. Folder structure and shared-file coordination

Keep the existing architecture. Read the current folder structure before creating files. Each member's screens, models, and feature services belong under their own `lib/features/` area; add subfolders only when useful.

The foundation originally used `m1_proposals_trips`, `m2_accommodation`, `m3_vehicles_destinations`, and `m4_users_contracts`. Recommended names after the ownership change are:

| Member | Recommended feature folder |
| --- | --- |
| M1 | `lib/features/m1_users_proposals_trips/` |
| M2 | `lib/features/m2_accommodation/` |
| M3 | `lib/features/m3_vehicles_destinations/` |
| M4 | `lib/features/m4_supplier_contracts/` |

These names are a coordinated cleanup plan, not a claim that renaming has occurred. Update folders, references, dashboard labels, tests, and the integration guide together before members diverge. If renaming is deferred, document the mapping and use existing folders; do not create competing copies.

Coordinate changes to `main.dart`, dashboard navigation, theme, API client, authentication, `pubspec.yaml`, `pubspec.lock`, Android configuration, and shared documentation. A member may add their agreed navigation entry, but should not overwrite another member's entry or rewrite the shared shell.

Reuse existing state management and navigation. Do not introduce a new state-management library, generic repository framework, dependency-injection system, or router for each component.

## 9. Coding style: simple, readable, and complete

- Use meaningful names such as `loadPendingHotels`, `approveVehicle`, and `selectedStatus`.
- Follow Dart conventions: `snake_case.dart` files, `UpperCamelCase` types, and `lowerCamelCase` variables/methods.
- Keep methods focused. Extract widgets when a screen becomes difficult to read or a real repeated UI pattern exists.
- Keep API calls and JSON conversion out of widget layout code. Feature services should reuse the shared client.
- Use small typed models for response data used by a feature. Limit `dynamic` maps to the JSON boundary; do not scatter raw JSON keys across widgets.
- Handle nullable and malformed fields intentionally. Avoid unexplained null assertions and false defaults for required fields.
- Use `async`/`await` clearly. Clear loading state on both success and failure, check `mounted` before updating disposed widgets, and dispose controllers/listeners.
- Prefer existing dependencies and straightforward functions over speculative abstraction or clever code.
- Comment on business rules or non-obvious reasons. Do not narrate every obvious line.
- Apply `dart format`. Do not disable analyzer rules merely to hide errors.

**Complete code means a working user flow.** It includes navigation, models, API integration, validation, loading/empty/error states, success feedback, refreshed data, role restrictions, and relevant verification. It does not mean implementing every possible future feature.

No production fake records, hardcoded counts, success messages without successful requests, unfinished buttons, or TODO-only required behavior. Mock data is appropriate in tests. If required backend support is missing, report the blocked part; do not fake completion or silently drop it.

## 10. API contracts, authentication, and errors

1. Verify exact routes, HTTP methods, body fields, response shape, enums, pagination fields, and required headers from current code.
2. The established `ApiResponse` flag is **`success`**, not `isSuccess`. The earlier wrong key broke live login while mock tests passed. Build test fixtures from the real contract, not from the Flutter implementation's assumptions.
3. Do not add compatibility with invented response formats. Handle genuine differences only when supported by code evidence.
4. Read HTTP status and the documented response body appropriately. Responses may contain objects, lists, strings, pagination wrappers, empty bodies, validation errors, or infrastructure errors; do not assume every response has the same payload.
5. Use the common API client and configuration. Do not add per-feature token stores, hardcoded laptop IPs, or duplicate `/api` URL segments.
6. Preserve session restore/logout behavior. HTTP 401 on a protected request should use the shared session-expiry path. HTTP 403 means insufficient permission and should not automatically be treated as session expiry.
7. Handle invalid login separately from expiry of an existing authenticated session.
8. Keep timeouts and network error handling consistent with the shared client. Never leave an endless spinner. Do not automatically retry approval, deletion, or other mutations without an established safe retry contract.
9. Display a useful backend validation/conflict message where safe. A parse error or denied action must not be mislabeled as a network failure.
10. Never log passwords, tokens, password hashes, connection strings, or full sensitive responses. Debug diagnostics can record a route, status code, and sanitized error type.
11. Keep local HTTP allowances debug-only. Do not disable certificate verification or weaken release settings to make a development request work.

Dates, money, totals, inventory availability, and state transitions remain backend-controlled. Format them clearly in the UI, preserve API timezone semantics, and do not invent currency or reinterpret booking dates as arbitrary local timestamps.

## 11. UI behavior expected in every component

- Reuse the common theme, typography, spacing, button styles, and status treatment.
- Keep the dashboard's four ownership labels consistent with section 3. Within each area, use clear user-facing page names.
- Support readable phone layouts, scrolling, keyboard-safe forms, and practical touch targets. Avoid overflow with long names or larger text.
- Every list needs initial loading, empty state, error with retry, refresh, and pagination when the endpoint is paginated. Do not load only the first page and call it “all records.”
- Only provide filters/search that work. A filter over the currently loaded page must not pretend to search all server records.
- Every form needs field validation matching the contract, submission progress, duplicate-submit prevention, recoverable errors, and clear success feedback.
- Confirm approval/rejection and destructive actions with the target's name and consequence. Ask for a reason when required; do not invent mandatory backend fields.
- After a successful mutation, refresh the affected detail/list and remove or update stale pending items.
- If another Admin has already changed a record, explain the conflict and reload its current state.
- Hide or disable actions based on role and known state, while handling backend rejection correctly.
- Missing optional information should say “Not provided” or equivalent. An empty result should not look like a failed request, and a failed request should not look like an empty result.

## 12. Team implementation and Git workflow

1. Verify the shared foundation has been merged into and shared from the team's `development` branch. Do not assume a local commit is available to teammates.
2. Coordinate ownership labels/folders and guide updates before starting independent work.
3. Each member starts from the agreed updated `development` commit and uses a separate feature branch, for example `feature/flutter-m1`, `feature/flutter-m2`, `feature/flutter-m3`, or `feature/flutter-m4`.
4. Implement a complete vertical slice: list → details → supported actions → refresh/error handling → tests. Then continue to the next feature in the member's scope.
5. Keep changes focused. Do not commit generated APKs, build/cache directories, secrets, or unrelated files. Preserve tracked application lockfiles according to repository conventions.
6. Coordinate shared-file edits and backend fixes. A Flutter task does not silently authorize changing backend business rules, database schemas, or another member's implementation.
7. Review changes and test before merging through the team's agreed review process. Resolve conflicts by preserving both intended behaviors and rerun affected checks.
8. Delete a feature branch only after its work is safely integrated and shared. Never force-delete unmerged work as routine cleanup.

No automatic push, merge, or branch deletion by coding assistants unless that action is explicitly authorized for the task.

## 13. Running and verifying on Android

Use the Flutter version agreed by the team and recorded in the current integration guide. Do not upgrade the SDK or packages independently during feature work.

For the established Windows USB workflow:

1. Start the ASP.NET Core backend from `server/TourManagement.Api` with `dotnet run`. Confirm it is listening on the expected HTTP port; the verified local workflow used `5160`.
2. Connect the phone with USB debugging enabled and authorize the laptop. Run `flutter devices` and copy your own device ID.
3. In PowerShell, configure USB forwarding, replacing `YOUR_DEVICE_ID`:

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" -s YOUR_DEVICE_ID reverse tcp:5160 tcp:5160
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" -s YOUR_DEVICE_ID reverse --list
```

4. From `mobile/tour_management_mobile`, run:

```powershell
flutter pub get
flutter run -d YOUR_DEVICE_ID --dart-define=API_BASE_URL=http://127.0.0.1:5160/api
```

Keep the backend running and USB connected. Reapply forwarding after disconnection/restart if it is lost. The phone's `127.0.0.1` reaches the laptop port only through the forwarding rule. `10.0.2.2` is for the Android emulator, not a physical-phone default.

If Flutter is not on PATH, use its actual installation path. On the initially tested laptop, that was `C:\src\flutter\bin\flutter.bat`; teammates must not assume their installation is identical. Restart the Flutter run when changing `--dart-define` values.

Required feature verification from the Flutter project directory:

```powershell
dart format lib test
flutter analyze
flutter test
flutter build apk --debug --dart-define=API_BASE_URL=http://127.0.0.1:5160/api
```

The APK command above builds for the USB development setup. It is not a standalone deployed app configuration. A distributable app needs an agreed reachable backend URL and appropriate release configuration.

Wait for command completion and record actual results. `flutter format` is not the formatter command. A started/background build is not a successful build. A successful APK build does not establish that live feature requests work.

## 14. Completion checklist and handoff

A member's component is complete only when its agreed scope is implemented or a specific limitation is explicitly accepted by the project owner.

- [ ] All agreed screens are reachable from the correct dashboard area.
- [ ] Lists, details, forms, and actions use real backend data and exact contracts.
- [ ] No required flow is left as a placeholder, TODO, fake response, or disconnected button.
- [ ] Loading, empty, error, retry, validation, refresh, and pagination behavior works where applicable.
- [ ] Role restrictions and record-state restrictions are respected; SuperAdmin-only controls are tested separately.
- [ ] Focused tests cover meaningful success/failure paths, response parsing, and important action restrictions. Fixtures match the real backend; tests do not merely mirror a wrong implementation.
- [ ] Formatting, analysis, tests, and the configured Android debug build have completed successfully, or exact failures are reported.
- [ ] The main feature flow is manually checked on Android against the running backend using agreed development data.
- [ ] Shared login, session restore, logout, and navigation still work after integration changes.
- [ ] Changed shared files and any backend dependencies are documented.
- [ ] Git diff contains only intended changes; no secrets or generated build outputs are included.

The handoff report must state: implemented flows, changed files, branch/commit, verification commands/results, device checks, backend changes if authorized, unresolved gaps, and exact remaining manual checks. Separate inspection, mocked tests, and live verification. Do not write “fully tested” or “no blockers” without evidence for the claimed scope.

## 15. Open items to resolve without blocking unrelated work

| Item | Required handling |
| --- | --- |
| Foundation merge and push | Verify current Git state; the last report did not establish completion |
| Revised M1/M4 labels and folders | Coordinate one shared update; do not duplicate old/new implementations |
| Contract duration validation | Enforce 1/2/3 choices in the UI; coordinate backend enforcement before claiming the business rule is complete |
| Contract renewal/direct creation | Check real lifecycle and DTOs; ask before exposing behavior that differs from the agreed request-after-expiry/termination rule |
| Competing contract approvals | Check transaction/constraint behavior before claiming one-contract enforcement under concurrency |
| AI hold expiry and payment outcome | Read current backend implementation; use returned states and timestamps rather than assumptions |
| Booking details and optional actions | Verify available authorized data; initial oversight is read-only unless an additional action is agreed |

Continue with unaffected screens while a specific question is resolved. Simple implementation should remain complete for the agreed scope, and uncertainty should remain visible rather than becoming hidden behavior.
