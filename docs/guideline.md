# Intelligent Travel Planner — Development Guideline

## Purpose

Keep this campus project simple, readable and easy to explain. This file records our agreed payment behaviour and how to finish the current work. It is not a request to rebuild the project.

Read this guideline before working on checkout or payments. Read the actual source code and relevant project documents too. Earlier reports describe what was claimed; the current code shows what is implemented.

## How to work

- Reuse existing services, components and conventions where practical.
- Choose straightforward code and clear names. Avoid unnecessary layers, frameworks or large refactors.
- You can inspect the full repository, so choose sensible implementation details yourself. Ask me only when a business rule is unclear, requirements conflict, or a change would alter an agreed decision. Do not guess silently.
- Preserve unrelated work. Inspect Git status before editing and avoid broad restore, reset or clean commands that discard changes.
- Keep credentials in the existing secret/environment configuration. Do not print them or put real secrets in tracked files.
- Explain progress and results in plain language. Separate completed work, unverified work and blocked work.
- Do not commit or push unless I request it.

## Agreed payment rules

| Item | Decision |
| --- | --- |
| Gateway | PayHere sandbox only; no real payments |
| Payment screen | React website only for now |
| Online amount | One LKR 1,000 website booking-confirmation fee per trip |
| Who keeps the fee | The website |
| Provider charges | Paid directly to the hotel, vehicle provider and supplier in full |
| Confirmation | One backend-verified payment confirms all bookings included in the shared checkout |
| Reservation period | One shared 12-hour hold |
| Before payment | Traveler can cancel |
| After payment | Traveler cannot add/remove bookings, cancel bookings or cancel the trip |
| No bookable services | Skip checkout; no website fee |
| Refunds | No refund system for this campus version |

**Example:** Provider charges are LKR 40,000. The website fee is LKR 1,000. The overall cost is LKR 41,000, but **PayHere charges only LKR 1,000**. Paying this fee must not mark provider charges as paid.

## Booking flow

### Manual trips

1. Traveler selects hotels, a vehicle and supplies as needed.
2. Selections are not reserved yet.
3. Traveler reviews them and clicks **Reserve all bookings**.
4. Backend rechecks availability, prices and stock, then creates all holds together with one 12-hour deadline. If any item fails, do not leave partial reservations.
5. Traveler pays the LKR 1,000 fee through PayHere sandbox.
6. Backend verifies payment and confirms all included bookings together.

Support the existing multi-hotel flow and valid combinations of services, including a single service type. Supplier reservations must join checkout and release their stock when an unpaid hold expires or is cancelled, without restoring stock twice.

### AI trips

Preserve the existing sequence: proposal → traveler acceptance → admin approval → shared hold → payment → confirmation. Do not reserve anything while planning or waiting for approval. Supplies remain outside AI planning.

## Payment behaviour

- Calculate the fee and verify payment on the backend using current official PayHere documentation.
- A browser redirect, query parameter such as `payment=success`, or frontend callback is not proof of payment. Show success only after reading verified backend status.
- Check ownership when starting payment or reading private checkout details.
- Repeated clicks or repeated gateway notifications must not create duplicate holds or confirm bookings twice. Allow failed-payment retries while the original hold is valid.
- Coordinate confirmation, cancellation and expiry so they cannot leave conflicting booking or stock states.
- Disable payment after expiry and reject expired payment initiation in the backend.
- Keep one small defensive check for a late successful notification: record the payment, but do not confirm expired/cancelled bookings. Show that payment was received but booking was not confirmed. No refund automation or extra admin subsystem is needed.
- Enforce paid-booking restrictions on the backend, including requests coming from Flutter. Protect booking dates, rooms, vehicles and quantities. Harmless fields such as the trip display title need not be locked unnecessarily.

## Current work to finish

The latest report says implementation files remain, but tests fail to compile, payment-result feedback is incomplete and migration application is unconfirmed. Verify these findings in the current repository; they are not permanent assumptions.

1. Repair test setup for the current service dependencies without weakening assertions or broadly replacing fixtures.
2. Complete payment-result feedback using actual backend status, including pending verification and unsuccessful outcomes.
3. Diagnose the configured database connection and establish whether the payment migration was applied. Do not assume a local database or recreate the database to fix a connection error.
4. Check the shared hold, supplier stock release and post-payment restrictions. Fix concrete gaps rather than redesigning working features.

## Verification and handoff

Run the relevant backend tests and React build. Add focused tests for the LKR 1,000 charge, invalid payment notifications, duplicate notifications, expiry, shared confirmation, supplier stock release and paid-booking restrictions. Check that existing AI approval and multi-hotel behaviour still work.

Use appropriate database-backed checks for concurrency where available. State clearly if those checks could not run. Existing tests passing does not establish that new payment behaviour is correct.

Finish with a short summary of files changed, migration status, actual test/build results and remaining setup steps. If sandbox credentials or a reachable notification URL are missing, complete the unblocked work and explain what I need to provide privately. Do not claim an end-to-end sandbox payment worked until it was actually tested.
