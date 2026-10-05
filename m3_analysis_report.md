# M3: Transport & Weather Analysis Report

## 1. Current M3 Workflow & Inputs/Outputs
- **Current State**: M3 is entirely a placeholder (`m3_transport_weather_node`). It returns empty lists for vehicles and weather without reading inputs.
- **Inputs Available**: M3 receives `state["input_snapshot"]` (StartDate, EndDate, GroupSize, Budget, Destinations), `state["plan"]` (from M1), and `state["accommodation_summary"]` (from M2).
- **Outputs Expected**: M4 reads `state["vehicles"]` and extracts the first vehicle to construct the `CreateCheckoutDto` (specifically `VehicleCheckoutItemDto`). It ignores `state["weather"]` entirely.
- **Tools**: The internal client (`client.py`) exposes `search_vehicles` and `get_weather`. M3 only has read-only access and cannot place holds or bookings.

## 2. Vehicle Search & Pricing (Backend Trace)
- **Route & Fields**: Python client calls `POST /api/internal/vehicles/search`, mapped to `InternalAgentController.SearchVehicles`, utilizing `VehicleService.SearchAsync`. It sends `StartDate`, `EndDate`, and `MinCapacity`. It returns `VehicleId`, `VehicleType`, `Model`, `Capacity`, and `PricePerDay`.
- **Identity & Rules**: Vehicles represent a "rental-with-driver" service (no self-drive). The backend filters out vehicles that have overlapping `Confirmed` or active `Held` bookings. 
- **1-Day Trip Pricing Bug**: Rental days are calculated as `(EndDate.Date - StartDate.Date).TotalDays` in `CheckoutService.cs`. For a 1-day trip, `EndDate` cannot exceed the trip's `EndDate` (which is the same date), meaning booking dates must fall on the exact same date. This results in `0` rental days, pricing the vehicle at **0 LKR (Free)**.
- **Multiple Vehicles**: The backend `CreateCheckoutDto` supports only one vehicle (`VehicleCheckoutItemDto? Vehicle`). M4 explicitly grabs only the first vehicle (`state.get("vehicles", [None])[0]`).

## 3. Weather Integration
- **Implementation**: **None.** The `InternalAgentController` explicitly returns `501 Not Implemented` for weather requests.
- **Behavior**: Calls to the internal client's `get_weather` will raise an HTTP error. M3's placeholder currently sets `state["weather"] = {}`, which M4 ignores. 

## 4. M2-to-M3 Connection & Failure Handling
- **Budget Reading**: M3 currently does not read `accommodation_summary.remaining_budget` as it is a placeholder.
- **M2 Failure Protection**: M2 failure does **not** prevent M3 from executing. LangGraph executes nodes sequentially. If M2 fails, `state["accommodation_summary"]` remains unset, meaning M3 will crash if it tries to access it blindly.
- **State Persistence**: If M4 (or M3) fails, M4 sets `final_payload = "{}"`. The actual detailed outputs from M1 and M2 (the daily itinerary and hotel allocations) only exist in-memory and are **lost** upon failure. Only the brief `ExecutionSummary` string is saved to the database. 

## 5. Owner Decisions Required
1. **Pickup Coordinates**: The checkout payload strictly requires `PickupLatitude` and `PickupLongitude`, but backend `Destination` records do not have coordinates. 
   - *Option A*: Let M3 guess or hardcode `0.0` since proposals are reviewed anyway (Easiest).
   - *Option B*: Add coordinates to the `Destination` database model (Harder but correct).
   - *Recommendation*: **Option A** for now to unblock M3, or update the DTO to make them optional.
2. **1-Day Trip Vehicle Pricing**: The backend charges 0 LKR for same-day rentals.
   - *Option A*: Fix `CheckoutService.cs` to use `Math.Max(1, days)`.
   - *Option B*: Let M3 send dates that span to the next day and remove the backend constraint restricting vehicle dates to the trip dates.
   - *Recommendation*: **Option A**, fix the backend calculation.
3. **Weather Provider**: We need to implement the backend weather service.
   - *Option A*: Use Open-Meteo (Free, no API key).
   - *Option B*: Use OpenWeatherMap (Requires API keys and secrets management).
   - *Recommendation*: **Option A**, Open-Meteo is simplest to integrate.

## 6. Offline Checks Run
- Grepped `CheckoutService.cs` for price calculations and date validations.
- Traced `AgentProposalResult` serialization in `WorkflowService.cs` to verify if intermediate states survive failure (they do not).
- Inspected `InternalAgentController.cs` to confirm the weather endpoint throws a `501`.

