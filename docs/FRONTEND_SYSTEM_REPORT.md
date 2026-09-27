# Complete Frontend System Specification & Architecture Report

## 1. Executive Summary & Frontend Architecture

The frontend of the **Intelligent Travel Planning & Tour Management Platform** consists of two client applications:
1. **Web Single Page Application (`client/`)**: Built with **React 18**, **Vite 5**, **React Router v6**, **Axios**, and a **custom Vanilla CSS design system** with vibrant colors, dark mode support, glassmorphism, and micro-animations. It provides specialized dashboards for Travelers, Hotel Owners, Transport Providers, Suppliers, and Admins.
2. **Mobile Cross-Platform Application (`mobile/tour_management_mobile/`)**: Built with **Flutter (Dart)**, providing mobile access to user authentication, trip planning, and booking services.

---

### Tech Stack & Core Technologies (Web SPA)
- **Framework & Build Tool**: React 18 / Vite 5
- **Routing**: `react-router-dom` v6 with dynamic nested routes & role-based route protection
- **State Management**: React Context API (`AuthContext.jsx`) synchronized with browser `localStorage`
- **HTTP Client**: Axios with request/response interceptors for automatic JWT Bearer token attachment (`api.js`, `hotelOwnerApi.js`, `transportProviderApi.js`, `profileApi.js`)
- **Iconography**: Lucide React (`lucide-react`)
- **Styling**: Vanilla CSS using modern CSS variables, CSS grid/flexbox layouts, responsive breakpoints, glassmorphism cards, and CSS micro-animations.

---

## 2. Comprehensive Frontend Tree File Structure & Purpose of Each File

```
Intelligent-Travel-Planning-Tour-Management/
├── client/                                        # Primary Web Frontend Application
│   ├── .env.example                              # Environment variable template (VITE_API_BASE_URL)
│   ├── .env.local                                # Local dev environment overrides
│   ├── .gitignore                                # Git ignore rules for node_modules and dist/
│   ├── .oxlintrc.json                            # Linter configuration file
│   ├── index.html                                # Root HTML entrypoint containing #root mount node
│   ├── package.json                              # Package manifest (react, vite, axios, react-router-dom, lucide-react)
│   ├── package-lock.json                         # Dependency lockfile
│   ├── README.md                                 # Client execution instructions (`npm run dev`)
│   ├── vite.config.js                            # Vite dev server and proxy configuration
│   ├── public/                                   # Public static assets served at root URL
│   │   ├── 1.jpg - 6.jpg                         # Destination cards & gallery images
│   │   ├── favicon.svg                           # Browser favicon
│   │   ├── Hero.jpg                              # Landing page main hero banner image
│   │   ├── logo.png                              # Brand logo asset
│   │   ├── icons.svg                             # SVG icon bundle
│   │   └── Nine arch Bridge.jpg                  # Demodara Nine Arch Bridge featured attraction asset
│   └── src/                                      # Source code directory
│       ├── main.jsx                              # React DOM render root, mounts BrowserRouter & AuthProvider
│       ├── App.jsx                               # Root application layout container
│       ├── index.css                             # Global resets, CSS variables, typography, dark theme
│       ├── App.css                               # App shell layout and container utility styles
│       ├── assets/                               # Bundled graphic assets
│       │   ├── hero.png                          # Hero graphic
│       │   ├── logo.png                          # Logo graphic
│       │   ├── react.svg                         # React icon asset
│       │   └── vite.svg                          # Vite icon asset
│       ├── components/                           # Shared Reusable Atomic & Layout Components
│       │   ├── Button.jsx                        # Standard button primitive (variants: primary, secondary, danger, outline; loading state)
│       │   ├── ConfirmDialog.jsx                 # Confirmation modal for destructive actions (deletion, cancellation)
│       │   ├── DashboardLayout.jsx               # Responsive layout sidebar, topbar, breadcrumbs & role menu
│       │   ├── DataTable.jsx                     # Styled tabular data display with sorting, status badges, and action menus
│       │   ├── EmptyState.jsx                    # Empty list placeholder graphic & CTA
│       │   ├── ErrorBanner.jsx                   # Notification alert & error banner component
│       │   ├── FeatureCard.jsx                   # Glassmorphism feature highlight card component
│       │   ├── ImageCard.jsx                     # Destination & tour card component with hover zoom effect
│       │   ├── Input.jsx                         # Styled text/password input primitive with error message handling
│       │   ├── LoadingSpinner.jsx                # Asynchronous spinner / loading overlay component
│       │   ├── Modal.jsx                         # Flexible modal overlay component with header, body, and action buttons
│       │   ├── PickupLocationModal.jsx           # Interactive modal for viewing and picking transport pickup location
│       │   ├── ProgressBar.jsx                   # Progress indicator bar component
│       │   ├── SearchFilterBar.jsx               # Search input & status drop-down filter bar
│       │   ├── StatusBadge.jsx                   # Color-coded status badge component (Active, Pending, Suspended, Confirmed, Cancelled)
│       │   └── SummaryMetricCard.jsx             # KPI metric card for summary dashboards (total revenue, total bookings, active rooms)
│       ├── context/                              # Global React Context
│       │   └── AuthContext.jsx                   # Global Auth state provider (user, token, role, login, register, logout)
│       ├── features/                             # Modular Domain Feature Components & Dashboards
│       │   ├── hotel-owner/                      # Hotel Owner Dashboard Feature Module
│       │   │   ├── components/
│       │   │   │   ├── AddEditHotelModal.jsx     # Form modal for creating or editing a hotel property
│       │   │   │   └── RoomModal.jsx             # Form modal for adding or editing room inventory details
│       │   │   ├── hooks/
│       │   │   │   └── useMyHotels.js            # Custom hook managing hotel owner property list & loading state
│       │   │   └── pages/
│       │   │       ├── MyHotelsPage.jsx          # Dashboard page listing owner's registered hotels
│       │   │       ├── HotelDetailPage.jsx       # Property details view with associated room management tables
│       │   │       ├── RoomsPage.jsx             # Aggregate room inventory page listing rooms across all owned hotels
│       │   │       ├── BookingsPage.jsx          # Customer room reservations table page
│       │   │       └── ProfilePage.jsx           # Owner profile settings & account deletion eligibility view
│       │   ├── transport-provider/               # Transport Provider Dashboard Feature Module
│       │   │   ├── components/
│       │   │   │   └── AddEditVehicleModal.jsx   # Form modal for registering or updating fleet vehicles
│       │   │   └── pages/
│       │   │       ├── VehiclesPage.jsx          # Vehicle fleet management page
│       │   │       └── BookingsPage.jsx          # Vehicle reservations overview page (`/api/vehicle-bookings/my-vehicles`)
│       │   └── shared/                           # Shared Cross-Role Feature Views
│       │       └── pages/
│       │           └── ProfilePage.jsx           # Reusable user profile management & safe account deletion page
│       ├── pages/                                # Top-Level Router Page Views
│       │   ├── HomePage.jsx                      # Primary landing page featuring hero section, destination cards & AI planner promo
│       │   ├── LoginPage.jsx                     # Authentication login view with role-based dashboard redirection
│       │   ├── RegisterPage.jsx                  # Account registration view supporting role selection (Traveler, Owner, Provider, Supplier)
│       │   └── DashboardPlaceholder.jsx         # Fallback placeholder view for user dashboards
│       ├── routes/                               # Router Configuration & Guards
│       │   ├── AppRoutes.jsx                     # Centralized router mapping paths (`/`, `/login`, `/register`, `/hotel-owner/*`, `/transport-provider/*`)
│       │   └── ProtectedRoute.jsx                # Auth & Role guard wrapper enforcing user login and role authorization
│       ├── services/                             # API Communication Layer
│       │   ├── api.js                            # Base Axios client with automatic Bearer token header injection
│       │   ├── hotelOwnerApi.js                  # API client for hotel owner properties, rooms, and bookings
│       │   ├── transportProviderApi.js           # API client for transport provider vehicles and fleet bookings
│       │   └── profileApi.js                     # API client for user profile updates and deletion eligibility checks
│       ├── styles/                               # CSS Design System
│       │   └── global.css                        # Design system tokens, color palettes, utility classes, animations
│       └── utils/                                # Helpers & Utilities
│           ├── constants.js                      # Shared constants (API endpoints, role keys, storage keys)
│           └── formatters.js                     # Formatting helpers for currency (LKR/USD), dates, and status strings
└── mobile/                                        # Mobile Application Project (Flutter)
    └── tour_management_mobile/
        ├── pubspec.yaml                          # Flutter dependencies manifest
        └── lib/
            ├── main.dart                         # Flutter entrypoint
            ├── app.dart                          # Root MaterialApp widget & route mapping
            ├── core/
            │   ├── constants/app_constants.dart # App constants
            │   ├── network/api_client.dart      # HTTP API client wrapper
            │   └── theme/app_theme.dart          # Mobile theme & color tokens
            ├── routes/app_routes.dart            # Mobile route definitions
            └── services/auth_service.dart        # Mobile authentication service
```

---

## 3. UI Component Architecture & Design System

The frontend implements a state-of-the-art UI design system featuring vibrant colors, micro-animations, glassmorphism, responsive navigation, and modular dashboard components:

### 3.1 Design System Tokens (`src/styles/global.css` & `src/index.css`)
- **Primary Color Palette**: Emerald Green & Cyan Gradients (`hsl(160, 84%, 39%)`, `hsl(187, 85%, 43%)`).
- **Dark Theme Palette**: Deep Charcoal & Slate background tones (`#0F172A`, `#1E293B`, `#334155`).
- **Status Colors**:
  - `Active` / `Approved` / `Confirmed`: Emerald Green (`#10B981`)
  - `Pending`: Amber Warning (`#F59E0B`)
  - `Suspended` / `Cancelled` / `Rejected`: Crimson Red (`#EF4444`)
  - `Draft` / `Inactive`: Slate Gray (`#64748B`)

### 3.2 Core UI Components
- **`DashboardLayout.jsx`**: Provides a standard responsive sidebar navigation layout across all provider and admin portals. Features dynamic breadcrumbs, role badges, active route highlighting, and collapsible sidebar.
- **`DataTable.jsx`**: Standardized data grid supporting column sorting, custom cell renderers, status badges, pagination, and multi-action drop-down menus.
- **`StatusBadge.jsx`**: Auto-formats entity statuses into styled badge chips with matching icon indicators.
- **`SummaryMetricCard.jsx`**: Visual KPI card rendering metric values, trend percentages, and iconography.
- **`ConfirmDialog.jsx`**: Accessible modal dialog prompting user confirmation before completing destructive operations.

---

## 4. Role-Based Feature Modules & User Workflows

### 4.1 Hotel Owner Feature Module (`src/features/hotel-owner/`)
- **`MyHotelsPage.jsx`**: View all registered hotel properties, pending admin approvals, and quick stats. Includes search bar and status filtering.
- **`HotelDetailPage.jsx`**: Overview of a single hotel property with property stats, manager details, and nested room inventory tables (`AddEditHotelModal.jsx`, `RoomModal.jsx`).
- **`RoomsPage.jsx`**: Aggregate view listing all rooms across all owned hotel properties with filter capabilities.
- **`BookingsPage.jsx`**: View customer room bookings placed across all owner properties with date ranges, guest counts, pricing, and cancellation status.
- **`ProfilePage.jsx`**: Owner profile overview with self-deletion protection check (`deletion-eligibility`).

### 4.2 Transport Provider Feature Module (`src/features/transport-provider/`)
- **`VehiclesPage.jsx`**: Manage vehicle fleet inventory (add, edit, suspend, soft-delete). Uses `AddEditVehicleModal.jsx`.
- **`BookingsPage.jsx`**: View customer reservations across all owned fleet vehicles using `transportProviderApi.getFleetBookings()`.
- **`PickupLocationModal.jsx`**: Map preview and location selector for transport pickup coordinates.

### 4.3 Shared Profile & Account Safety Module (`src/features/shared/`)
- **`ProfilePage.jsx`**: Allows any logged-in user to update profile details (name, phone number) and execute safe account deletion. Calls `/api/profile/me/deletion-eligibility` before enabling account deletion CTA.

---

## 5. Client Routing & Security Architecture

The routing layer (`src/routes/AppRoutes.jsx`) uses `react-router-dom` and is protected by `ProtectedRoute.jsx`:

```jsx
// Example Route Configuration
<Routes>
  {/* Public Routes */}
  <Route path="/" element={<HomePage />} />
  <Route path="/login" element={<LoginPage />} />
  <Route path="/register" element={<RegisterPage />} />

  {/* Hotel Owner Routes */}
  <Route path="/hotel-owner" element={
    <ProtectedRoute allowedRoles={['HotelOwner']}>
      <DashboardLayout role="HotelOwner" />
    </ProtectedRoute>
  }>
    <Route path="hotels" element={<MyHotelsPage />} />
    <Route path="hotels/:id" element={<HotelDetailPage />} />
    <Route path="rooms" element={<RoomsPage />} />
    <Route path="bookings" element={<BookingsPage />} />
    <Route path="profile" element={<ProfilePage />} />
  </Route>

  {/* Transport Provider Routes */}
  <Route path="/transport-provider" element={
    <ProtectedRoute allowedRoles={['TransportProvider']}>
      <DashboardLayout role="TransportProvider" />
    </ProtectedRoute>
  }>
    <Route path="vehicles" element={<VehiclesPage />} />
    <Route path="bookings" element={<TransportBookingsPage />} />
    <Route path="profile" element={<ProfilePage />} />
  </Route>
</Routes>
```

---

## 6. API Services Layer & HTTP Interceptors

1. **`api.js`**:
   - Axios instance with base URL bound to `import.meta.env.VITE_API_BASE_URL` (defaulting to `http://localhost:5000` / `http://localhost:5271`).
   - Request Interceptor: Automatically inspects `localStorage` for `token` and injects `Authorization: Bearer <token>`.
   - Response Interceptor: Catches HTTP 401 Unauthorized responses and redirects user to `/login`.
2. **`hotelOwnerApi.js`**: Dedicated functions for `getMyHotels()`, `getHotelById()`, `getMyRooms()`, `createHotel()`, `updateHotel()`, `createRoom()`, `updateRoom()`, `getBookings()`.
3. **`transportProviderApi.js`**: Functions for `getMyVehicles()`, `createVehicle()`, `updateVehicle()`, `deactivateVehicle()`, `getFleetBookings()`.
4. **`profileApi.js`**: Functions for `getMyProfile()`, `updateProfile()`, `checkDeletionEligibility()`, `deleteAccount()`.
