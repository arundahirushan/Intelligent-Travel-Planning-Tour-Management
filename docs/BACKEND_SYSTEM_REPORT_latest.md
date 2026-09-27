# Complete Backend System Specification & Architecture Report

## 1. Executive Summary & Architecture Overview

The backend of the **Intelligent Travel Planning & Tour Management Platform** is designed as a hybrid microservice-assisted monolithic API architecture. It comprises two main services:
1. **Primary Backend API (`server/TourManagement.Api`)**: Built with **ASP.NET Core 8.0 (C#)**, providing RESTful API endpoints for user authentication, profile management, trip planning, hotel and room bookings, transport vehicle management and reservations, supplier item catalogs, contract management, admin approvals, account deletion safety guards, and AI workflow coordination.
2. **AI Microservice (`ai-service`)**: Built with **Python 3.11+**, **FastAPI**, and **LangGraph**, providing multi-agent workflow execution (planning, recommendations, weather advisory, supplier validation). It is an **internal-only microservice** called strictly via the gateway ASP.NET Core API (`AgentServiceClient.cs`).
3. **Automated Test Suite (`server/TourManagement.Api.Tests`)**: Built with **xUnit**, **Moq**, and **Entity Framework Core In-Memory Database** to validate business logic, role-based security, account deletion safety rules, and booking constraints.

---

### Tech Stack & Core Technologies
- **Framework**: .NET 8.0 ASP.NET Core Web API / Python 3.11 FastAPI
- **Database**: PostgreSQL (Entity Framework Core 8 `Npgsql` provider)
- **Security**: JWT (JSON Web Tokens) with Role-Based Access Control (RBAC)
- **AI Agent Framework**: LangGraph StateGraph, LangChain
- **API Documentation**: OpenAPI / Swagger UI (integrated with JWT Bearer auth support)
- **Error Handling**: Global Exception Handling Middleware with standardized `ApiResponse<T>` JSON wrappers.

---

### User Roles & Permissions Matrix
The backend enforces 6 primary user roles defined in `Common/Constants/Roles.cs`:
1. **`Traveler`**: Standard user who creates trips, requests draft itineraries, books hotel rooms, reserves transport vehicles, and places supply orders.
2. **`HotelOwner`**: Service provider who registers and manages hotels, room inventories across properties, room availability, and views/manages room bookings.
3. **`TransportProvider`**: Service provider who registers vehicles, updates vehicle status, views fleet bookings across all owned vehicles (`GET /api/vehicle-bookings/my-vehicles`), and manages vehicle reservations.
4. **`Supplier`**: Service provider who submits contract requests to admins, publishes supply items for sale, updates inventory, and manages incoming supply orders.
5. **`Admin`**: Platform manager who approves/rejects pending service providers (Hotel Owners, Transport Providers, Suppliers), pending hotels, pending vehicles, contract requests, and oversees platform resources.
6. **`SuperAdmin`**: Platform owner who has full administrative privileges, including creating/promoting other Admin accounts and removing accounts.

---

## 2. Comprehensive Backend Tree File Structure & Purpose of Each File

```
Intelligent-Travel-Planning-Tour-Management/
├── server/
│   ├── TourManagement.sln                         # Visual Studio / .NET Solution File uniting API & Test projects
│   ├── TourManagement.Api/                        # Main ASP.NET Core 8.0 Web API Project
│   │   ├── TourManagement.Api.csproj             # Project dependencies (.NET 8, EF Core, Npgsql, JWT Bearer, Swagger)
│   │   ├── TourManagement.Api.http               # HTTP test requests file for IDE testing
│   │   ├── Program.cs                             # Application entry point, DI container configuration, middleware pipeline setup
│   │   ├── appsettings.json                       # Base application settings (JWT defaults, logging config)
│   │   ├── appsettings.Development.json           # Development-specific environment settings
│   │   ├── Properties/
│   │   │   └── launchSettings.json                # Kestrel web server launch profiles, ports, environment variables
│   │   ├── AgentIntegration/                      # Communication bridge to the Python AI service
│   │   │   ├── IAgentServiceClient.cs            # Interface for invoking external AI agent workflows
│   │   │   ├── AgentServiceClient.cs             # HttpClient implementation communicating with ai-service FastAPI
│   │   │   └── Models/
│   │   │       └── WorkflowDtos.cs                # Request/Response DTOs for AI workflow execution and status
│   │   ├── Common/                                # Shared utilities, constants, exceptions, response wrappers
│   │   │   ├── ApiResponse.cs                     # Generic standard API response structure (Success, Data, Message, Errors)
│   │   │   ├── PagedResult.cs                     # Pagination helper model for list endpoints
│   │   │   ├── DateRangeHelper.cs                 # Utility functions for validating and checking overlap of dates
│   │   │   ├── Constants/
│   │   │   │   └── Roles.cs                       # Defines role string constants (Traveler, HotelOwner, TransportProvider, etc.)
│   │   │   └── Exceptions/
│   │   │       ├── NotFoundException.cs           # Custom HTTP 404 Exception thrown when entity is missing
│   │   │       ├── ForbiddenException.cs          # Custom HTTP 403 Exception thrown for unauthorized access
│   │   │       └── ValidationException.cs         # Custom HTTP 400 Exception thrown for business rule violations
│   │   ├── Configurations/                        # Strongly-typed configuration POCO classes bound from appsettings
│   │   │   └── AppSettings.cs                     # Contains JwtSettings, PayHereSettings, and AiServiceSettings classes
│   │   ├── Controllers/                           # REST API Controllers handling HTTP requests
│   │   │   ├── AuthController.cs                 # User registration and authentication endpoints (/api/auth)
│   │   │   ├── BookingsController.cs             # Legacy placeholder redirecting to HotelBookingsController
│   │   │   ├── ContractRequestsController.cs     # Supplier contract onboarding requests (/api/contract-requests)
│   │   │   ├── ContractsController.cs            # Admin contract management & supplier status (/api/contracts)
│   │   │   ├── DestinationsController.cs         # Destination management and catalog endpoints (/api/destinations)
│   │   │   ├── HotelBookingsController.cs       # Traveler hotel room reservations & owner property bookings (/api/hotel-bookings)
│   │   │   ├── HotelsController.cs               # Hotel property & room inventory management (/api/hotels)
│   │   │   ├── ProfileController.cs              # Authenticated user profile viewing/editing & deletion guards (/api/profile)
│   │   │   ├── SuppliesController.cs             # Supplier product listing & browsing (/api/supplies)
│   │   │   ├── SupplyOrdersController.cs         # Traveler supply order placement & supplier management (/api/supply-orders)
│   │   │   ├── TripsController.cs                # Trip creation, itinerary item management & AI generation (/api/trips)
│   │   │   ├── UsersController.cs                # User management, admin creation, provider approvals (/api/users)
│   │   │   ├── VehicleBookingsController.cs      # Transport vehicle reservations & provider fleet bookings (/api/vehicle-bookings)
│   │   │   ├── VehiclesController.cs             # Transport provider vehicle fleet management (/api/vehicles)
│   │   │   └── WorkflowsController.cs           # Gateway controller for AI agent workflow interactions (/api/workflows)
│   │   ├── Data/                                  # Database access layer
│   │   │   ├── AppDbContext.cs                   # EF Core DbContext containing entity sets and Fluent API relationships
│   │   │   └── SeedData.cs                        # Async seeder initializing SuperAdmin account on startup
│   │   ├── Dtos/                                  # Data Transfer Objects organized by domain feature
│   │   │   ├── Accommodation/                    # Hotel & Room DTOs (HotelDetailDto, HotelSummaryDto, RoomDto, RoomWithHotelDto)
│   │   │   ├── Auth/                             # Login & Register request/response DTOs
│   │   │   ├── Destination/                      # Destination CRUD DTOs
│   │   │   ├── Profile/                          # UpdateProfileDto & DeletionEligibilityDto
│   │   │   ├── Supplier/                         # Supply items, contracts, & contract request DTOs
│   │   │   ├── SupplyOrders/                     # Supply order creation, update, & summary DTOs
│   │   │   ├── Transport/                        # Vehicle catalog, search, and VehicleSummaryDto metadata DTOs
│   │   │   ├── Trips/                            # Trip & itinerary management DTOs
│   │   │   └── User/                             # User admin creation & summary DTOs
│   │   ├── Extensions/                            # Dependency Injection extensions
│   │   │   └── ServiceCollectionExtensions.cs    # Method extensions registering JWT Auth, services & deletion guards
│   │   ├── Mappings/                              # Object mapping extension profiles
│   │   │   └── RoomMappings.cs                    # Extension methods mapping Room entities to/from DTOs (ToDto, ToRoomWithHotelDto, ToEntity)
│   │   ├── Middleware/                            # ASP.NET Core request pipeline middleware
│   │   │   └── ExceptionHandlingMiddleware.cs    # Global error interceptor mapping custom exceptions to clean HTTP responses
│   │   ├── Models/                                # Entity Framework domain entities & domain enums
│   │   │   ├── User.cs & UserStatus.cs            # User account entity and state enum (Pending, Active, Rejected)
│   │   │   ├── Trip.cs & TripStatus.cs            # Travel trip plan entity and state enum (Draft, Confirmed, Cancelled, Completed)
│   │   │   ├── ItineraryItem.cs                  # Schedule item belonging to a Trip
│   │   │   ├── Destination.cs                    # Geographic destination entity
│   │   │   ├── Hotel.cs                           # Hotel property entity
│   │   │   ├── Room.cs & RoomStatus.cs            # Hotel room inventory item & status (Available, Occupied, Maintenance)
│   │   │   ├── HotelBooking.cs                   # Room reservation entity linked to Traveler and Room
│   │   │   ├── Vehicle.cs & VehicleStatus.cs      # Vehicle entity & status (Active, Inactive, Maintenance, Suspended)
│   │   │   ├── VehicleBooking.cs                 # Vehicle reservation entity linked to Traveler and Vehicle
│   │   │   ├── VehicleBookingStatus.cs           # Status enum for vehicle bookings (Confirmed, Cancelled, Completed)
│   │   │   ├── Supply.cs & SupplyStatus.cs        # Supplier catalog product & status (Published, Unpublished, Removed)
│   │   │   ├── SupplyOrder.cs & SupplyStatus.cs  # Order entity placed by traveler for supply items
│   │   │   ├── ContractRequest.cs                # Supplier application request entity (Pending, Approved, Rejected)
│   │   │   ├── Contract.cs                        # Active legal contract binding a Supplier to the platform
│   │   │   └── RemovalReason.cs                   # Enum documenting reason for administrative supply removal
│   │   ├── Profile/                               # Account Deletion Guard strategy implementations
│   │   │   ├── IAccountDeletionGuard.cs          # Interface defining rules for safe account self-deletion
│   │   │   ├── HotelOwnerDeletionGuard.cs        # Prevents Hotel Owner deletion if active/future room bookings exist
│   │   │   ├── TransportProviderDeletionGuard.cs # Prevents Transport Provider deletion if active/future vehicle bookings exist
│   │   │   └── SupplierDeletionGuard.cs          # Prevents Supplier deletion if active contracts or orders exist
│   │   ├── Repositories/                          # Data repository layer interfaces & implementations
│   │   │   ├── Interfaces/
│   │   │   └── Implementations/
│   │   └── Services/                              # Core business logic layer
│   │       ├── Interfaces/                        # Service interfaces (IAuthService, ITripService, IProfileService, etc.)
│   │       └── Implementations/                   # Business logic implementations injecting AppDbContext & deletion guards
│   └── TourManagement.Api.Tests/                  # Unit and Integration Test Suite
│       ├── TourManagement.Api.Tests.csproj       # Test project file (xUnit, Moq, EF Core In-Memory)
│       ├── UnitTest1.cs                           # Basic test verification runner
│       └── Services/                              # Domain service unit tests
│           ├── AuthServiceTests.cs                # Password hashing, JWT token generation, & login/register validation
│           ├── BookingUpdateAndDeleteTests.cs     # Booking cancellation, modification, and refund rules
│           ├── BusinessRuleTests.cs               # Business constraints and state transition validation
│           ├── ContractServiceTests.cs            # Supplier contract request approval/rejection lifecycle
│           ├── HotelOwnerAggregateEndpointsTests.cs # Hotel owner multi-property & room aggregation tests
│           ├── HotelServiceTests.cs               # Hotel search, room availability calculation, and filtering
│           ├── OccupancyTests.cs                  # Room capacity and overlap collision detection
│           ├── ProfileServiceTests.cs             # Deletion guard checks (Hotel Owner, Transport Provider, Supplier)
│           ├── SupplyOrderServiceTests.cs         # Supply stock reduction, order updates, and cancellation
│           └── VehicleServiceTests.cs             # Fleet management & TransportProvider vehicle booking queries
└── ai-service/                                    # Internal Python LangGraph AI Service
    ├── .env.example                               # Environment variable template (OpenAI/Anthropic API keys)
    ├── main.py                                    # FastAPI application entrypoint with health & workflow routes
    ├── requirements.txt                           # Python dependencies (fastapi, uvicorn, langgraph, langchain, pydantic)
    ├── agents/                                    # Autonomous specialized agent implementations
    │   ├── __init__.py                            # Package initialization
    │   ├── planning_agent.py                      # Multi-day itinerary generation agent
    │   ├── recommendation_agent.py                # Accommodation & transport recommendation agent
    │   ├── weather_agent.py                       # External weather forecast & advisory fetching agent
    │   └── validation_agent.py                    # Contract compliance & constraint validation agent
    ├── graph/                                     # LangGraph workflow definitions
    │   └── workflow_graph.py                      # StateGraph wiring nodes and edges between agents
    ├── schemas/                                   # Request/Response Pydantic validation models
    ├── state/                                     # Shared state definitions
    │   └── workflow_state.py                      # TypedDict / Pydantic state passed through the agent pipeline
    ├── tests/                                     # Pytest test suite for AI agents
    └── tools/                                     # Custom tools (Weather API wrappers, Database query helpers)
```

---

## 3. Database & Entity Domain Models Summary

The database uses PostgreSQL via Entity Framework Core (`AppDbContext`). Key entities and relationships:

1. **`User`**: Core identity entity with `Email`, `PasswordHash`, `Role` (Traveler, HotelOwner, TransportProvider, Supplier, Admin, SuperAdmin), and `Status` (Pending, Active, Rejected).
2. **`Trip` & `ItineraryItem`**: Represents a traveler's planned trip containing multiple `ItineraryItem` entries (activities, locations, start/end timestamps, cost estimation).
3. **`Destination`**: Tourist destinations and locations referenced in itineraries.
4. **`Hotel` & `Room`**: Represents hotel properties owned by a `HotelOwner`. A `Hotel` owns multiple `Room` items mapped via `RoomMappings.cs`.
5. **`HotelBooking`**: Room reservation created by a `Traveler` for specific `CheckInDate` and `CheckOutDate` ranges.
6. **`Vehicle` & `VehicleBooking`**: Fleet vehicles owned by a `TransportProvider` and reservations made by `Traveler`. Providers can view all fleet reservations via `GET /api/vehicle-bookings/my-vehicles`.
7. **`Supply` & `SupplyOrder`**: Supplier catalog product entries (`Price`, `StockQuantity`) and orders placed by travelers (`Quantity`, `TotalPrice`, `OrderStatus`).
8. **`ContractRequest` & `Contract`**: Application submitted by a `Supplier`. Upon approval, a binding `Contract` is generated.

---

## 4. Complete REST API Specification (Updated)

### 4.1 Authentication API (`/api/auth`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Public | None | Registers a new user account. Service providers default to `Pending` status. |
| `POST` | `/api/auth/login` | Public | None | Authenticates user credentials and returns a JWT Bearer token containing User ID, Email, and Role. |

---

### 4.2 User Profile API (`/api/profile`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/profile/me` | JWT | Any | Returns full profile details of the authenticated user. |
| `PUT` | `/api/profile/me` | JWT | Any | Updates profile details (`Name`, `PhoneNumber`). |
| `GET` | `/api/profile/me/deletion-eligibility` | JWT | Any | Evaluates `IAccountDeletionGuard` rules to check if account can be safely deleted without active booking conflicts. |
| `DELETE` | `/api/profile/me` | JWT | Any | Self-deletes user account if account deletion guards pass without active dependency violations. |

---

### 4.3 User & Admin Management API (`/api/users`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/users/pending` | JWT | Admin, SuperAdmin | Fetches all pending service provider registration requests. |
| `POST` | `/api/users/{id}/approve` | JWT | Admin, SuperAdmin | Approves a pending service provider account. |
| `POST` | `/api/users/{id}/reject` | JWT | Admin, SuperAdmin | Rejects a pending service provider account. |
| `GET` | `/api/users` | JWT | Admin, SuperAdmin | Lists all registered users with optional role filtering. |
| `POST` | `/api/users/admins` | JWT | SuperAdmin | Creates a new Admin user account directly. |
| `POST` | `/api/users/{id}/promote` | JWT | SuperAdmin | Promotes an existing user to Admin role. |
| `DELETE` | `/api/users/{id}` | JWT | SuperAdmin | Admin force-deletion of any user account. |

---

### 4.4 Trips & Itinerary API (`/api/trips`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/trips` | JWT | Traveler | Creates a new trip header (Title, Destination, Dates, Budget). |
| `GET` | `/api/trips/my` | JWT | Traveler | Retrieves all trips owned by the authenticated traveler. |
| `GET` | `/api/trips/{id}` | JWT | Any Auth | Retrieves full trip details and itinerary items (ownership verified in service layer). |
| `PUT` | `/api/trips/{id}` | JWT | Traveler | Updates trip basic details. |
| `DELETE` | `/api/trips/{id}` | JWT | Traveler | Deletes a trip and its itinerary items. |
| `POST` | `/api/trips/{id}/itinerary-items` | JWT | Traveler | Adds a manual activity/item to a trip itinerary. |
| `PUT` | `/api/trips/{id}/itinerary-items/{itemId}` | JWT | Traveler | Updates an existing itinerary item. |
| `DELETE` | `/api/trips/{id}/itinerary-items/{itemId}` | JWT | Traveler | Removes an item from the itinerary. |
| `POST` | `/api/trips/{id}/generate-draft-itinerary` | JWT | Traveler | Triggers AI agent workflow to generate a draft itinerary for the trip. |
| `GET` | `/api/trips` | JWT | Admin, SuperAdmin | Admin overview of all traveler trips. |
| `POST` | `/api/trips/{id}/force-cancel` | JWT | Admin, SuperAdmin | Admin force-cancellation of an invalid or disputed trip. |

---

### 4.5 Hotel & Room Inventory API (`/api/hotels`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/hotels` | JWT | HotelOwner | Registers a new hotel property (defaults to pending admin approval). |
| `GET` | `/api/hotels/my` | JWT | HotelOwner | Fetches all hotels owned by the logged-in Hotel Owner. |
| `GET` | `/api/hotels/my/{id}` | JWT | HotelOwner | Fetches specific hotel property details tailored for owner. |
| `GET` | `/api/hotels/my/rooms` | JWT | HotelOwner | **NEW**: Fetches all rooms across all properties owned by the Hotel Owner. |
| `PUT` | `/api/hotels/{id}` | JWT | HotelOwner | Updates hotel property information. |
| `DELETE` | `/api/hotels/{id}` | JWT | HotelOwner | Removes hotel property if no active bookings exist. |
| `POST` | `/api/hotels/{hotelId}/rooms` | JWT | HotelOwner | Adds a room to a hotel property. |
| `PUT` | `/api/hotels/{hotelId}/rooms/{roomId}` | JWT | HotelOwner | Updates room details, price, or capacity. |
| `DELETE` | `/api/hotels/{hotelId}/rooms/{roomId}` | JWT | HotelOwner | Removes a room from inventory. |
| `GET` | `/api/hotels/{hotelId}/bookings` | JWT | HotelOwner | Views all customer bookings for a specific hotel. |
| `GET` | `/api/hotels` | JWT | Admin, SuperAdmin | Views all hotels in the platform database. |
| `GET` | `/api/hotels/pending` | JWT | Admin, SuperAdmin | Lists pending hotel properties awaiting admin approval. |
| `POST` | `/api/hotels/{id}/approve` | JWT | Admin, SuperAdmin | Admin approves hotel property. |
| `POST` | `/api/hotels/{id}/reject` | JWT | Admin, SuperAdmin | Admin rejects hotel property. |
| `POST` | `/api/hotels/{id}/suspend` | JWT | Admin, SuperAdmin | Admin suspends hotel property due to policy violations. |
| `GET` | `/api/hotels/bookings-all` | JWT | Admin, SuperAdmin | Admin global view of all hotel room bookings across the system. |
| `GET` | `/api/hotels/search` | JWT | Any Auth | Public/Traveler search for hotels by destination, date range, and guest capacity. |
| `GET` | `/api/hotels/{id}` | JWT | Any Auth | Retrieves detailed view of a hotel and available room types. |

---

### 4.6 Hotel Bookings API (`/api/hotel-bookings`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/hotel-bookings` | JWT | Traveler | Reserves a room for specified check-in/out dates with capacity validation. |
| `GET` | `/api/hotel-bookings/my` | JWT | Traveler | Fetches all room reservations made by the logged-in traveler. |
| `GET` | `/api/hotel-bookings/my-hotels` | JWT | HotelOwner | Fetches room bookings across all properties managed by owner. |
| `POST` | `/api/hotel-bookings/{id}/cancel` | JWT | Any Auth | Cancels a hotel booking. |
| `PUT` | `/api/hotel-bookings/{id}` | JWT | Traveler | Modifies reservation dates (subject to room availability). |
| `DELETE` | `/api/hotel-bookings/{id}` | JWT | Any Auth | Removes a cancelled booking record. |

---

### 4.7 Transport Vehicles API (`/api/vehicles`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/vehicles` | JWT | TransportProvider | Registers a vehicle into provider's fleet (pending admin approval). |
| `GET` | `/api/vehicles/my` | JWT | TransportProvider | Lists all vehicles owned by logged-in transport provider. |
| `GET` | `/api/vehicles/my/{id}` | JWT | TransportProvider | Gets detailed info for a specific provider vehicle. |
| `PUT` | `/api/vehicles/{id}` | JWT | TransportProvider | Updates vehicle attributes (model, capacity, daily rate). |
| `DELETE` | `/api/vehicles/{id}` | JWT | TransportProvider | Soft-deletes a vehicle from fleet (`Status = Inactive`). |
| `GET` | `/api/vehicles/{vehicleId}/bookings` | JWT | TransportProvider | Views reservations placed for a specific vehicle. |
| `GET` | `/api/vehicles` | JWT | Admin, SuperAdmin | System-wide list of all vehicles. |
| `GET` | `/api/vehicles/pending` | JWT | Admin, SuperAdmin | Pending vehicle registrations awaiting admin review. |
| `POST` | `/api/vehicles/{id}/approve` | JWT | Admin, SuperAdmin | Approves vehicle for listing. |
| `POST` | `/api/vehicles/{id}/reject` | JWT | Admin, SuperAdmin | Rejects vehicle registration. |
| `POST` | `/api/vehicles/{id}/suspend` | JWT | Admin, SuperAdmin | Suspends vehicle listing. |
| `GET` | `/api/vehicles/search` | JWT | Any Auth | Traveler search for available vehicles by location and seating capacity. |
| `GET` | `/api/vehicles/{id}` | JWT | Any Auth | Retrieves details for a specific vehicle. |

---

### 4.8 Vehicle Bookings API (`/api/vehicle-bookings`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/vehicle-bookings` | JWT | Traveler | Reserves a vehicle for specified start/end rental dates. |
| `GET` | `/api/vehicle-bookings/my` | JWT | Traveler | Retrieves traveler's vehicle reservations. |
| `GET` | `/api/vehicle-bookings/my-vehicles` | JWT | TransportProvider | **NEW**: Retrieves all vehicle reservations across all fleet vehicles owned by the Transport Provider. |
| `POST` | `/api/vehicle-bookings/{id}/cancel` | JWT | Any Auth | Cancels a vehicle reservation. |
| `PUT` | `/api/vehicle-bookings/{id}` | JWT | Traveler | Updates vehicle reservation dates. |
| `DELETE` | `/api/vehicle-bookings/{id}` | JWT | Any Auth | Deletes a vehicle booking record. |
| `GET` | `/api/vehicle-bookings` | JWT | Admin, SuperAdmin | Global list of all vehicle reservations. |

---

### 4.9 Supplier Catalog & Products API (`/api/supplies`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/supplies` | JWT | Supplier | Creates a supply product listing (requires active contract). |
| `GET` | `/api/supplies/my` | JWT | Supplier | Retrieves products listed by the authenticated supplier. |
| `GET` | `/api/supplies/my/{id}` | JWT | Supplier | Retrieves specific supplier product details. |
| `PUT` | `/api/supplies/{id}` | JWT | Supplier | Updates product price, description, or stock quantity. |
| `DELETE` | `/api/supplies/{id}` | JWT | Supplier | Unpublishes/deletes a product listing. |
| `POST` | `/api/supplies/{id}/republish` | JWT | Supplier | Republishes an unpublished supply item. |
| `GET` | `/api/supplies` | JWT | Admin, SuperAdmin | Views all supply listings across suppliers. |
| `POST` | `/api/supplies/{id}/remove` | JWT | Admin, SuperAdmin | Forcefully removes a supply product (attaching `RemovalReason`). |
| `GET` | `/api/supplies/browse` | JWT | Any Auth | Traveler search and browse interface for published travel gear and supplies. |

---

### 4.10 Supply Orders API (`/api/supply-orders`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/supply-orders` | JWT | Traveler | Places an order for travel gear, reducing available inventory stock. |
| `GET` | `/api/supply-orders/my` | JWT | Traveler | Fetches all supply orders placed by traveler. |
| `POST` | `/api/supply-orders/{id}/cancel` | JWT | Traveler, Admin, SuperAdmin | Cancels a supply order and restores product stock. |
| `PUT` | `/api/supply-orders/{id}` | JWT | Traveler | Modifies supply order quantity or delivery address. |
| `DELETE` | `/api/supply-orders/{id}` | JWT | Traveler, Admin, SuperAdmin | Deletes an order record. |
| `GET` | `/api/supply-orders/received` | JWT | Supplier | Retrieves incoming orders placed for supplier's products. |
| `GET` | `/api/supply-orders` | JWT | Admin, SuperAdmin | Global view of all supply orders in the system. |

---

### 4.11 Supplier Contracts & Applications API (`/api/contract-requests` & `/api/contracts`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/contract-requests` | JWT | Supplier | Submits a formal application request to become an authorized platform supplier. |
| `GET` | `/api/contract-requests/my` | JWT | Supplier | Views supplier's submitted contract application status. |
| `GET` | `/api/contract-requests/{id}` | JWT | Any Auth | Views details of a specific contract request. |
| `GET` | `/api/contract-requests` | JWT | Admin, SuperAdmin | Views all pending supplier application requests. |
| `POST` | `/api/contract-requests/{id}/approve` | JWT | Admin, SuperAdmin | Approves application and automatically creates an active binding `Contract`. |
| `POST` | `/api/contract-requests/{id}/reject` | JWT | Admin, SuperAdmin | Rejects supplier contract application. |
| `GET` | `/api/contracts` | JWT | Admin, SuperAdmin | Lists all active and historical supplier contracts. |
| `GET` | `/api/contracts/{id}` | JWT | Admin, SuperAdmin | Gets contract details by ID. |
| `POST` | `/api/contracts/{id}/terminate` | JWT | Admin, SuperAdmin | Terminates an active supplier contract immediately. |
| `GET` | `/api/suppliers/{supplierId}/contract-status` | JWT | Admin, SuperAdmin | Checks if a supplier holds an active valid contract. |

---

### 4.12 Destinations API (`/api/destinations`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/destinations` | JWT | Any Auth | Retrieves list of all destinations available for travel planning. |
| `GET` | `/api/destinations/{id}` | JWT | Any Auth | Retrieves specific destination details and attractions. |
| `POST` | `/api/destinations` | JWT | Admin, SuperAdmin | Adds a new destination to the global catalog. |
| `PUT` | `/api/destinations/{id}` | JWT | Admin, SuperAdmin | Updates destination information and metadata. |
| `DELETE` | `/api/destinations/{id}` | JWT | Admin, SuperAdmin | Deletes a destination record. |

---

### 4.13 AI Workflow Gateway API (`/api/workflows`)
| Method | Endpoint | Auth | Role | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/workflows/start` | JWT | Traveler | Initiates a multi-agent AI execution flow via `AgentServiceClient`. |
| `GET` | `/api/workflows/{id}/status` | JWT | Traveler | Queries current state of an ongoing LangGraph workflow execution. |
| `POST` | `/api/workflows/{id}/approve` | JWT | Traveler | Approves generated AI recommendations/itinerary for integration into Trip. |

---

## 5. Python AI Microservice Deep Dive (`ai-service`)

The `ai-service` acts as an autonomous multi-agent reasoning engine:
1. **`main.py`**: Exposes FastAPI endpoints for workflow execution.
2. **`graph/workflow_graph.py`**: Defines the sequential/conditional execution path between four distinct agents:
   - **`planning_agent`**: Reads traveler budget, destination, and duration to formulate a raw itinerary outline.
   - **`recommendation_agent`**: Interrogates hotel and vehicle availability data to suggest optimal stay & transit options.
   - **`weather_agent`**: Fetches weather forecast data for travel dates to attach advisories (e.g. rain gear alerts).
   - **`validation_agent`**: Verifies that proposed recommendations comply with active supplier contracts and budget limits.
3. **`state/workflow_state.py`**: Tracks execution state across agent nodes using Pydantic models.

---

## 6. Verification & Test Suite Summary

The test project `TourManagement.Api.Tests` ensures high reliability across all domain services:
- **`AuthServiceTests.cs`**: Verifies JWT claim creation, PBKDF2/BCrypt password hashing security, and unauthorized login handling.
- **`HotelServiceTests.cs` & `OccupancyTests.cs`**: Validates date overlap math for room booking prevention and maximum guest capacity enforcement.
- **`HotelOwnerAggregateEndpointsTests.cs`**: **NEW**: Tests multi-hotel room querying and aggregate booking lookups for hotel owners.
- **`VehicleServiceTests.cs`**: **NEW**: Validates transport fleet availability, date collision detection, and provider fleet booking queries (`GetMyVehiclesBookingsAsync`).
- **`ProfileServiceTests.cs`**: **NEW**: Verifies polymorphic account deletion safeguards (`IAccountDeletionGuard`) for Hotel Owners, Transport Providers, and Suppliers.
- **`ContractServiceTests.cs` & `SupplyOrderServiceTests.cs`**: Validates supplier contract approvals and stock inventory management under concurrency.
