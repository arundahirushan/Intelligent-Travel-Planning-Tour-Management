# Coding Guidelines — Intelligent Travel Planning & Tour Management

**Read this file before writing any backend code in this project.** It
captures decisions that apply across every component, so all 4 members'
work stays consistent even when built by different people on different
machines.

If anything you're about to do conflicts with a rule here, or a
requirement is ambiguous, **stop and ask the project owner rather than
guessing** — these patterns get copied across all 4 components, so a
silent deviation in one place creates inconsistency everywhere else.

---

## Project Identity
- Code name: **TourManagement** (used for the .NET namespace/solution,
  Flutter package name, etc.) — the actual repo name has hyphens and isn't
  valid as a code identifier.
- **Sri Lanka only.** No multi-country support anywhere. All prices are in
  **LKR**. Do not add currency selection/conversion or country fields.
  `Destination.Region` is a free-text field (e.g. "Southern Province"),
  not a country.

## Code Style
- Simple and readable over clever. No unnecessary design patterns, no
  one-liners that are hard to follow.
- Comment the *why*, not the *what*, where a decision isn't obvious.
- A student with basic C# knowledge should be able to open any file and
  understand it without external explanation.

## Architecture (layered, always in this order)
```
Controller → Service → Repository → AppDbContext → PostgreSQL
```
- Controllers are thin: parse request → call one service method → map
  result to HTTP status → return. No business logic in controllers.
- Services hold all business logic, status transitions, ownership checks,
  and the "business-specific operation" each component requires.
- Repositories are the *only* layer that touches `AppDbContext`. Generic
  `IRepository<T>` (GetById/GetAll/Add/Update/Delete) plus specific
  repositories only where extra queries are genuinely needed.

## Authentication & Authorization
- **No ASP.NET Core Identity.** A plain `User` entity + BCrypt password
  hashing + manually issued JWT.
- JWT expires after 2 hours. No refresh token — kept deliberately simple.
- Roles are **string constants** in `Common/Constants/Roles.cs` (not a C#
  enum), referenced in `[Authorize(Roles = Roles.X)]` attributes.
- Coarse-grained access (which roles can call an endpoint) →
  `[Authorize(Roles = ...)]`.
- Fine-grained access (does this user own this specific resource?) → a
  plain `if` check inside the service layer comparing the resource's
  owner ID to the current user's ID (from JWT claims). No policy-handler
  framework — keep it visible and simple.

## Errors & Responses
- Three standard exceptions, thrown from services:
  `Common/Exceptions/NotFoundException.cs`,
  `ForbiddenException.cs`, `ValidationException.cs`.
- `Middleware/ExceptionHandlingMiddleware.cs` catches these and returns
  the right HTTP status (404/403/400) with one consistent JSON error
  shape. Anything unhandled becomes a generic 500 — never leak a stack
  trace to the client.
- Every successful response uses the shared `Common/ApiResponse.cs`
  wrapper: `{ success, data, message }`.

## Validation
- Basic checks (`[Required]`, `[Range]`, `[RegularExpression]`, etc.) live
  as Data Annotations on request DTOs.
- Cross-field / business rules (e.g. "end date after start date," "can't
  exceed stock") live in the **service layer**, and throw
  `ValidationException` with a clear message.

## DTOs & Mapping
- One folder per feature area under `Dtos/`.
- Mapping lives in the top-level **`Mappings/`** folder, one file per
  entity (e.g. `DestinationMappings.cs`, `TripMappings.cs`). No AutoMapper.
  Keep the mapping logic visible in code, not hidden behind configuration.
- Standard method naming convention:
  - `ToResponseDto()` — entity → response DTO
  - `ToEntity()` — create DTO → new entity
  - `UpdateFromDto()` — apply update DTO onto an existing entity

## Database Conventions
- Enums are stored as **strings**, not integers
  (`HasConversion<string>()`), so they're readable when inspecting the
  database directly.
- Prefer **soft deletes** (a status like `Inactive`/`Cancelled`/`Suspended`)
  over hard deletes for anything with history or relationships — trips,
  hotels, bookings, contracts, etc. Hard-deleting breaks referential
  history other records may still point to.
- Every entity has `CreatedAt` / `UpdatedAt`.
- Add indexes on any column used heavily in search/filter/sort (foreign
  keys, status columns, anything queried in a "my X" or "search" endpoint).
- Decide delete-behavior (cascade/restrict) deliberately for every
  relationship — don't accept EF Core's default without thinking about
  what it means for real data. If unsure, ask rather than guessing.

## Secrets & Configuration
- **Actual secret values** must never be in `appsettings.json`. Use
  `.NET User Secrets` locally, environment variables in production.
  Covers: JWT signing key, database connection string, PayHere secret,
  weather API key, AI-service URL + shared secret.
- `appsettings.json` may contain the **key structure** (e.g.
  `"JwtSettings": { "SecretKey": "" }`) with **empty string placeholders**
  so the shape is visible to developers — but the real values must always
  come from User Secrets / environment variables at runtime.
- Database is hosted on Supabase (PostgreSQL) — used *only* as a database
  host. Do not use Supabase's built-in Auth, auto-generated REST API, or
  client SDKs anywhere in this project.

## Agentic AI Integration
- The Python (`ai-service/`) is **internal-only**. Nothing outside
  `AgentIntegration/` in the API project should ever call it directly, and
  React/Flutter must never call it directly either — always through
  `TourManagement.Api`.

## API Documentation
- Swagger/OpenAPI enabled in **all** environments (not just dev), with a
  JWT Bearer auth option configured so a token can be pasted directly into
  Swagger UI to test protected endpoints.

## Testing
- Not exhaustive — focus tests on key business rules and edge cases
  (status transitions, ownership checks, capacity/validation limits), not
  100% coverage of every getter/setter.

## For AI Coding Assistants (Antigravity, etc.)
Before writing any new code in this project:
1. Read this file in full.
2. Read the actual existing files for shared infrastructure (`User.cs`,
   `Destination.cs`, `ApiResponse.cs`, `ExceptionHandlingMiddleware.cs`,
   `Roles.cs`, and any existing component you're extending) to match
   established patterns exactly — don't infer or reinvent them.
3. If a new requirement conflicts with something here, or is ambiguous,
   stop and ask before proceeding.
