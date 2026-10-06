# Intelligent Travel Planning & Tour Management System

**SE3090 · Integrated Full-Stack & Agentic AI Application Development**

---

## 1. Project Overview

The **Intelligent Travel Planning & Tour Management System** is an end-to-end, multi-tier software platform designed to handle the complete travel lifecycle. It covers custom itinerary planning, hotel property and room bookings, vehicle fleet rentals with drivers, travel gear and supply orders, supplier contract management, and administrative platform moderation.

### The Problem It Solves
Traditional travel booking platforms require users to manually search, coordinate, and balance budgets across separate hotel, transport, and activity providers. This platform combines:
- A customer and provider web portal (React) for travelers, hotel owners, transport providers, and suppliers.
- An administrative mobile application (Flutter) for platform managers to review pending registrations, moderate listings, and authorize high-impact AI travel proposals.
- A centralized C# ASP.NET Core 8 REST API gateway paired with a PostgreSQL database.
- An internal multi-agent AI pipeline (Python / FastAPI / LangGraph) that generates multi-day travel itineraries, recommends matching hotels and vehicles within budget, checks weather advisories, and enforces business validation rules.

---

## 2. User Roles & Responsibilities

The system enforces six user roles with strict Role-Based Access Control (RBAC):

| Role | Interface | Core Responsibilities |
| :--- | :--- | :--- |
| **`Traveler`** | React Web | Creates trips, generates and reviews AI itineraries, places hotel room bookings, reserves transport vehicles, purchases travel gear, and completes checkout payments. |
| **`HotelOwner`** | React Web | Registers hotel properties, manages room inventories, sets pricing/capacity, and tracks customer room reservations. |
| **`TransportProvider`** | React Web | Registers rental vehicles, manages fleet status, and views fleet bookings across all owned vehicles. |
| **`Supplier`** | React Web | Submits contract applications to the platform, publishes travel supply products, and manages incoming customer orders. |
| **`Admin`** | Flutter Mobile | Moderates pending provider accounts, approves/rejects hotel and vehicle listings, reviews supplier contract requests, terminates contracts, force-removes invalid listings, and reviews/approves AI travel proposals. |
| **`SuperAdmin`** | Flutter Mobile | Possesses full platform administrative privileges, including creating new Admin accounts, promoting users to Admin, and force-deleting eligible accounts. |

---

## 3. Business Components & Group Ownership

The application is structured into four primary business components, with individual ownership split across the four team members. 

> **Inventory vs. Booking Distinction:** Component owners M2, M3, and M4 manage the service-provider inventory, listings, and contract lifecycles. Component owner **M1** owns the customer-facing **Traveler Booking Flows** across all categories (hotels, vehicles, and supply orders), integrating them into traveler trip management and cart checkout.

| Component | Owner & Student ID | ASP.NET Core Backend Services | Database Entities | Frontends Owned | Distinct AI Contribution |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **M1: Trips, Itineraries, AI Orchestration & Traveler Booking Flows** | **Arunda Hirushan**<br>*(IT24103139)* | `TripsController`, `WorkflowsController`, `CheckoutController`, `HotelBookingsController` *(traveler flows)*, `VehicleBookingsController` *(traveler flows)*, `SupplyOrdersController` *(traveler flows)*, `TripService`, `WorkflowService`, `CheckoutService`, `AgentServiceClient` | `Trip`, `ItineraryItem`, `TripProposal`, `ExecutionSummary`, `TripCheckout`, `PaymentAttempt` | React: Traveler Trip Dashboard, Create Trip Wizard, Booking Modals & Checkout Cart Widget<br>Flutter: `m1_users_proposals_trips` (User Approvals, Trips Oversight, AI Proposal Review) | **Planning Agent (M1):** Generates day-by-day itineraries based on budget, duration, and group size. Orchestrates internal multi-agent graph. |
| **M2: Hotels & Accommodation Management** | **Chandupa Senanayaka**<br>*(IT24102678)* | `HotelsController`, `HotelService`, `HotelBookingService` *(owner property queries)* | `Hotel`, `Room`, `HotelBooking` | React: Hotel Owner Dashboard & Room Modal<br>Flutter: `m2_accommodation` (Hotel Moderation & Accommodation Booking Oversight) | **Accommodation Agent (M2):** Searches destination-specific hotels and allocates budget-compliant rooms. |
| **M3: Vehicles Fleet & Destinations Management** | **Chathuni Piyumali**<br>*(IT24101027)* | `VehiclesController`, `DestinationsController`, `VehicleService`, `DestinationService`, `InternalAgentController` | `Vehicle`, `VehicleBooking`, `Destination` | React: Vehicle Fleet Search & Destination Catalog<br>Flutter: `m3_vehicles_destinations` (Vehicle Moderation, Destination CRUD & Photo Upload) | **Transport & Weather Agent (M3):** Searches available rental vehicles and fetches weather forecasts. |
| **M4: Supplier Catalog & Contract Management** | **Kaveesha Paaris**<br>*(IT24102678)* | `SuppliesController`, `ContractRequestsController`, `ContractsController`, `SupplyService`, `ContractService`, `SupplyOrderService` *(supplier order management)* | `Supply`, `SupplyOrder`, `ContractRequest`, `Contract` | React: Supplier Product Management & Contract Application UI<br>Flutter: `m4_supplier_contracts` (Contract Applications, Supply Removal & Order Oversight) | **Validation Agent (M4):** Enforces budget rules, contract validity, and generates final checkout payload. |

---

## 4. Workflows & Booking Rules

### 4.1 Manual Booking Workflows
1. **Direct Hotel Reservation:** Travelers search available hotels by destination and guest count, select room types, and place reservations directly via `/api/hotel-bookings` (owned by M1 on traveler side, backed by M2 room inventory).
2. **Direct Transport Rental:** Travelers search active vehicles by seating capacity and dates, placing reservations directly via `/api/vehicle-bookings` (owned by M1 on traveler side, backed by M3 vehicle fleet).
3. **Supply Purchasing:** Travelers browse published travel gear from authorized suppliers and place orders via `/api/supply-orders` (owned by M1 on traveler side, backed by M4 supplier catalog). **Note:** Supply purchasing is owned by M1 on the traveler side, but supplies remain strictly outside the AI-generated proposal booking flow.

### 4.2 AI-Assisted Trip-Planning & Human-in-the-Loop Approval Workflow
```
[Traveler on React Web] ──(1) Input Trip Criteria / Budget──> [ASP.NET Core API Gateway]
                                                                      │
                                                          (2) Invoke Internal Flow
                                                                      ▼
                                                          [Python FastAPI / LangGraph]
                                                          ├─ M1: Planning Agent
                                                          ├─ M2: Accommodation Agent
                                                          ├─ M3: Transport & Weather Agent
                                                          └─ M4: Validation Agent
                                                                      │
                                                          (3) Return Validated Proposal
                                                                      ▼
[Traveler on React Web] <──(4) Review & Accept Proposal── [ASP.NET Core API Gateway]
         │                                                            │
  (Status = PendingAdminApproval)                                     │ (Persist State in PostgreSQL)
         │                                                            ▼
         └──────────────────────────────────────────────> [Admin on Flutter Mobile App]
                                                                      │
                                                          (5) Review & Click Approve
                                                                      ▼
                                                          [ASP.NET Core API Gateway]
                                                          └── (6) Create 12-Hour Hold
                                                                      │
[Traveler on React Web] <──(7) View 12-Hr Hold Cart ────────── (HoldPlaced)
         │
  (8) Complete PayHere Payment Checkout
         │
         ▼
[ASP.NET Core API Gateway] ──(9) Verify Signature ──> [Bookings Confirmed in PostgreSQL]
```

### Verified Business Rules
- **No Early Holds:** Rooms and vehicles are **not** held during initial AI proposal generation or while waiting for Traveler acceptance.
- **Admin Approval Required:** An AI proposal cannot be checked out until an Admin reviews and approves it in the Flutter mobile application.
- **12-Hour Availability Hold:** Upon Admin approval, ASP.NET Core verifies real-time room/vehicle availability and places a 12-hour hold (`HoldPlaced` status). If no bookings are required (e.g., day trips), the proposal transitions to `ApprovedNoBookingRequired`.
- **Automatic Hold Release:** An automated background service (`ExpiredHoldCleanupService`) periodically checks for and releases expired holds.
- **Backend-Verified Payment:** Booking status transitions to `Confirmed` only after ASP.NET Core verifies the PayHere payment gateway signature response/webhook.
- **Supplies Outside AI Flow:** Supplies booking belongs to M1 on the traveler side, but supplies remain strictly outside the AI-generated trip proposal booking flow.

---

## 5. Technology Stack & Rationale

| Layer | Technology | Rationale |
| :--- | :--- | :--- |
| **Public API Gateway** | **C# ASP.NET Core 8 Web API** | High performance, strong typing, structured middleware, and built-in JWT authentication/authorization pipeline. |
| **Relational Database** | **PostgreSQL & Entity Framework Core 8** | Robust ACID-compliant relational data storage with Npgsql EF Core migrations, LINQ queries, and transaction management. |
| **Web Application** | **React 19 (Vite, JavaScript)** | Fast client-side rendering, component modularity, React Router 7, and Context API for global state management. |
| **Mobile Application** | **Flutter & Dart** | Cross-platform mobile UI for Android with native widget performance and secure local token storage (`flutter_secure_storage`). |
| **Agentic AI Microservice** | **Python 3.11+, FastAPI, LangGraph** | Graph-based multi-agent orchestration (`StateGraph`), structured Pydantic state schema validation, and integration with Google Gemini LLMs. |

---

## 6. Repository Folder Structure

```
Intelligent-Travel-Planning-Tour-Management/
├── client/                                  # React Web Application (Vite)
│   ├── src/
│   │   ├── api/                             # Axios client & API endpoints
│   │   ├── components/                      # Shared UI components & Layouts
│   │   ├── context/                         # AuthContext & CheckoutContext
│   │   └── features/                        # Role-based feature views (traveler, hotel-owner, supplier, admin)
│   └── package.json
├── server/                                  # ASP.NET Core 8 Web API Solution
│   ├── TourManagement.sln
│   ├── TourManagement.Api/                  # Primary REST Web API project
│   │   ├── AgentIntegration/                # HttpClient client bridge for Python ai-service
│   │   ├── Controllers/                     # REST API Controllers
│   │   ├── Data/                            # AppDbContext & EF Core SeedData
│   │   ├── Dtos/                            # Domain Data Transfer Objects
│   │   ├── Middleware/                      # Global Exception Handling Middleware
│   │   ├── Models/                          # EF Core Entity Domain Models
│   │   ├── Profile/                         # Account Deletion Guard implementations
│   │   └── Services/                        # Business logic implementations
│   └── TourManagement.Api.Tests/            # xUnit Test Project (EF Core In-Memory)
├── mobile/tour_management_mobile/           # Flutter Mobile Application (Admin Portal)
│   ├── lib/
│   │   ├── core/                            # Shared theme, router, API client, secure storage
│   │   └── features/                        # Ownership modules (m1, m2, m3, m4)
│   ├── test/                                # Flutter unit & widget tests
│   └── pubspec.yaml
├── ai-service/                              # Internal Python LangGraph AI Service
│   ├── agents/                              # Autonomous specialized agents (m1 to m4)
│   ├── graph/                               # LangGraph workflow graph definition
│   ├── state/                               # Workflow TypedDict / Pydantic state
│   ├── tests/                               # Pytest suite
│   ├── main.py                              # FastAPI application entry point
│   └── requirements.txt
├── docs/                                    # System documentation & reports
│   ├── adr/                                 # Architecture Decision Records (.gitkeep)
│   └── er-diagram/                          # Database ER Diagram (.gitkeep)
└── .github/workflows/                       # GitHub Actions CI Workflows
    └── backend-ci.yml
```

---

## 7. System Integration & Architecture

### Communication Constraints
- **Shared API Gateway:** Both the React Web App and Flutter Mobile App communicate **exclusively** with the ASP.NET Core REST API.
- **Internal AI Service Isolation:** The Python `ai-service` is an internal microservice listening on `http://localhost:8000`. It is never accessed directly by client applications. ASP.NET Core communicates with it via `AgentServiceClient.cs`.

```
[React Web App]      [Flutter Mobile App]
       │                      │
       └─────── HTTPS / REST ──┘
                  │
                  ▼
       [ASP.NET Core REST API Gateway]
        ├── EF Core ──> [PostgreSQL Database]
        └── HttpClient ──> [Python AI Microservice (FastAPI/LangGraph)]
```

---

## 8. Agentic AI Subsystem Detail

The `ai-service` uses a 4-agent LangGraph `StateGraph` state-machine flow:

1. **Planning Agent (`m1_planning.py`):** Accepts trip parameters (destination, duration, budget, group size) and generates a structured day-by-day itinerary.
2. **Accommodation Agent (`m2_accommodation.py`):** Searches destination hotel properties via backend endpoints and allocates rooms fitting within the remaining budget.
3. **Transport & Weather Agent (`m3_transport_weather.py`):** Queries vehicle availability for the trip duration and fetches weather advisories.
4. **Validation Agent (`m4_validation.py`):** Interrogates ASP.NET Core internal validation (`POST /api/internal/proposal/validate`) to verify budget limits, active supplier contracts, and availability constraints, producing the final `CheckoutPayload`.

**State Persistence:** Workflow progress, execution summaries, and validation outputs are saved in PostgreSQL within `TripProposals` and `ExecutionSummaries` tables.

---

## 9. Prerequisites & System Requirements

Ensure the following tools are installed on your system before local setup:
- **.NET 8.0 SDK:** [Download .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js (v18+) & npm:** [Download Node.js](https://nodejs.org/)
- **Python (v3.11+):** [Download Python](https://www.python.org/)
- **Flutter SDK (v3.x):** [Download Flutter](https://docs.flutter.dev/get-started/install)
- **PostgreSQL Database (v15+):** Installed locally or hosted instance (e.g. Supabase / Railway).
- **Android Studio / Android SDK:** For running Flutter on Android device/emulator.

---

## 10. Environment Variable Configuration

### 10.1 Backend API (`server/TourManagement.Api/appsettings.json` or User Secrets)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=TourManagementDb;Username=postgres;Password=your_password"
  },
  "JwtSettings": {
    "Issuer": "TourManagementApi",
    "Audience": "TourManagementClients",
    "SecretKey": "YOUR_STRONG_JWT_SECRET_KEY_HERE_AT_LEAST_32_CHARS",
    "ExpiresInHours": 2
  },
  "AiServiceSettings": {
    "BaseUrl": "http://localhost:8000",
    "Secret": "dev-secret-do-not-use-in-prod"
  },
  "SupabaseStorage": {
    "Url": "https://your-supabase-instance.supabase.co",
    "ServiceRoleKey": "your_supabase_service_role_key",
    "Bucket": "listing-photos"
  },
  "PayHereSettings": {
    "MerchantId": "your_merchant_id",
    "MerchantSecret": "your_merchant_secret",
    "BaseUrl": "https://sandbox.payhere.lk"
  }
}
```

### 10.2 React Web Client (`client/.env.local`)
```env
VITE_API_BASE_URL=http://localhost:5160/api
```

### 10.3 Python AI Microservice (`ai-service/.env`)
```env
GEMINI_API_KEY=your_google_gemini_api_key
GEMINI_MODEL=gemini-3.5-flash-lite
AI_SECRET=dev-secret-do-not-use-in-prod
TOUR_MANAGEMENT_API_URL=http://localhost:5160/api/internal
PORT=8000
```

### 10.4 Flutter Mobile App (`mobile/tour_management_mobile`)
Configured via `--dart-define` at runtime:
```bash
--dart-define=API_BASE_URL=http://127.0.0.1:5160/api
```

---

## 11. Database Setup & EF Core Migrations

1. Configure your PostgreSQL connection string in `server/TourManagement.Api/appsettings.json` or user-secrets.
2. Apply Entity Framework Core migrations to create database tables and seed initial data:

**Windows (PowerShell):**
```powershell
cd server/TourManagement.Api
dotnet ef database update
```

**macOS / Linux:**
```bash
cd server/TourManagement.Api
dotnet ef database update
```

*Note: On initial startup, `SeedData.cs` automatically seeds the default `SuperAdmin` account if no administrative account exists.*

---

## 12. Local Startup Guide (Step-by-Step Order)

Follow this exact startup order to run the complete integrated stack locally:

### Step 1: Start PostgreSQL
Ensure PostgreSQL is running locally on port 5432 (or using your configured remote connection).

### Step 2: Start ASP.NET Core Web API
```bash
# Working Directory: server/TourManagement.Api
dotnet run
```
*API Kestrel server starts on `http://localhost:5160` (or configured port).*

### Step 3: Start Python AI Microservice
```bash
# Working Directory: ai-service
# Create and activate virtual environment (optional but recommended)
python -m venv venv
# Windows:
.\venv\Scripts\activate
# macOS/Linux:
source venv/bin/activate

pip install -r requirements.txt
python main.py
```
*FastAPI microservice starts on `http://localhost:8000`.*

### Step 4: Start React Web Application
```bash
# Working Directory: client
npm install
npm run dev
```
*Vite dev server starts on `http://localhost:5173`.*

### Step 5: Start Flutter Mobile Application (Android USB / Emulator)
If running on a physical Android device via USB debugging, set up port forwarding first:
```powershell
# Forward device port 5160 to laptop host port 5160
adb reverse tcp:5160 tcp:5160

# Working Directory: mobile/tour_management_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://127.0.0.1:5160/api
```

---

## 13. Running Automated Test Suites

### 13.1 Backend ASP.NET Core Tests (xUnit)
```bash
# Working Directory: repository root
dotnet test server/TourManagement.Api.Tests/TourManagement.Api.Tests.csproj
```

### 13.2 React Web Client Tests (Node test runner)
```bash
# Working Directory: client
npm test
```

### 13.3 Flutter Mobile App Tests (Dart test)
```bash
# Working Directory: mobile/tour_management_mobile
flutter test
```

### 13.4 Python AI Service Tests (Pytest)
```bash
# Working Directory: ai-service
# Windows (PowerShell):
$env:PYTHONPATH="."; python -m pytest tests

# macOS / Linux:
PYTHONPATH=. python -m pytest tests
```

---

## 14. API Documentation & Swagger

When running the ASP.NET Core API backend in Development mode, interactive OpenAPI / Swagger documentation is available at:
```
http://localhost:5160/swagger
```
The Swagger UI includes JWT Bearer authentication support (`Authorize` button). Obtain a Bearer token by posting credentials to `/api/auth/login` and enter `Bearer <your_token>` to test protected endpoints.

---

## 15. Deployment & Evaluator Access

Below are the official access links and resources for project evaluators:

- **GitHub Repository URL:** https://github.com/arundahirushan/Intelligent-Travel-Planning-Tour-Management.git
- **Deployed React Web App:** https://intelligent-travel-planning-tour-ma-three.vercel.app/
- **Deployed ASP.NET Core API Base URL:** https://intelligent-travel-planning-tour.onrender.com/api

### Test Account Credentials
Evaluators can use the following pre-configured test accounts to evaluate each system role:

| Role | Email | Password | Target Interface & Access Notes |
| :--- | :--- | :--- | :--- |
| **`SuperAdmin`** | `superadmin@tourmanagement.com` | `SuperAdmin@123` | **Flutter Mobile App** (Full privileges, Admin user creation, force-deletion) |
| **`Admin`** | `admin@tourmanagement.com` | `Admin123!` | **Flutter Mobile App** (Moderate listings, approve contracts & AI proposals) |
| **`Traveler`** | `traveler@example.com` | `Traveler123!` | **React Web App** (Trip creation, AI proposal checkout, direct bookings) |
| **`HotelOwner`** | `hotelowner@example.com` | `Owner123!` | **React Web App** (Property registration, room management, guest bookings) |
| **`TransportProvider`** | `transport@example.com` | `Driver123!` | **React Web App** (Fleet registration, vehicle management, rental bookings) |
| **`Supplier`** | `supplier@example.com` | `Supplier123!` | **React Web App** (Contract requests, supply product catalog, customer orders) |

---

## 16. Security Considerations

- **Credential Protection:** Real connection strings, JWT secret keys, API keys, and third-party merchant credentials are excluded from version control via `.gitignore`.
- **Password Security:** User passwords are hashed using industry-standard password hashing before persistence.
- **Role Authorization:** Protected backend endpoints strictly validate JWT claims using standard ASP.NET Core `[Authorize(Roles = ...)]` policy attributes.
- **Account Deletion Safety Guards:** Polymorphic guards (`IAccountDeletionGuard`) prevent provider self-deletion if active room bookings, vehicle rentals, or contracts exist.
- **Prompt Injection & AI Tool Safety:** Tool parameters are parsed and validated through Pydantic schemas in Python and DTOs in C#. The backend performs deterministic business validation overrides regardless of LLM outputs.

---

## 17. Detailed Documentation Links

- [Backend Architecture & System Report](docs/BACKEND_SYSTEM_REPORT_latest.md)
- [Frontend System & Architecture Report](docs/FRONTEND_SYSTEM_REPORT.md)
- [Flutter Development & Architecture Guide](docs/FLUTTER_DEVELOPMENT_GUIDE.md)
- [Hotel Owner Dashboard Specification](docs/HOTEL_OWNER_DASHBOARD_DOCS.md)
- [Booking Systems Specification](docs/BOOKING_SYSTEMS_SPECIFICATION.md)

*Note: Architecture Decision Records (ADRs) in `docs/adr/` and ER Diagrams in `docs/er-diagram/` are maintained as architectural baseline folders.*

---

## 18. AI Use Disclosure

This project was developed under **Level 4 — Full AI** guidelines of the CLEAR Framework (Perkins et al., 2024). AI tools (including agentic coding assistants and IDE copilots) were utilized during development for code generation, refactoring, and test creation.

All generated code, business logic, schemas, and test suites were reviewed, verified, and tested by the team members. Full details and individual AI usage logs are documented in the consolidated group submission report.
