# Hotel Owner Dashboard (M2) — Technical Documentation Report

## Executive Summary
This document provides a comprehensive technical overview of the **Hotel Owner Dashboard (M2)** built for the Intelligent Travel Planning & Tour Management System. It details the architecture, UI component structure, page workflows, routing, backend API integrations, and state management conventions so that developers and future AI assistants can seamlessly build upon or extend this codebase.

---

## 1. Architecture & Design System

### 1.1 Styling & Theme Configuration
The dashboard is styled using standard CSS variables and utility classes in `client/src/index.css` following a **blue palette** design system documented in `docs/FRONTEND_GUIDELINES.md`.

Key CSS custom properties used across all components:
- **Colors**:
  - `var(--primary)` (`#1E3D34` - Deep Forest Teal)
  - `var(--canvas)` (`#F8FAFC` - Main page background)
  - `var(--surface-neutral)` (`#F1F5F9`)
  - `var(--surface-blue)` (`#E0F2FE`)
  - Status indicators: `var(--status-success)` (`#2E7D32`), `var(--status-warning)` (`#E65100`), `var(--status-danger)` (`#C62828`), `var(--status-info)` (`#1565C0`), `var(--status-inactive)` (`#757575`).
- **Typography**:
  - Headings: `font-heading` (`Plus Jakarta Sans`)
  - Body: `font-body` (`Plus Jakarta Sans` / System Sans)
- **Spacing**:
  - `--space-xs` (4px), `--space-sm` (8px), `--space-md` (16px), `--space-lg` (24px), `--space-xl` (32px)

---

## 2. Shared UI Design System Components (`client/src/components/`)

These reusable components are decoupled from specific feature logic and can be reused by M1, M3, M4 modules:

| Component | Path | Description & Props |
| :--- | :--- | :--- |
| **`DashboardLayout`** | `client/src/components/DashboardLayout.jsx` | Master layout wrapper. Includes top navbar with user profile dropdown, mobile hamburger menu, floating help widget, and main content area. |
| **`StatusBadge`** | `client/src/components/StatusBadge.jsx` | Status indicator pill supporting `Active`, `PendingApproval`, `Suspended`, `Rejected`, `Inactive`, `Confirmed`, `Cancelled`, `Completed`. |
| **`ProgressBar`** | `client/src/components/ProgressBar.jsx` | Linear occupancy/progress bar with customizable percentage, color variants, and optional label text. |
| **`SummaryMetricCard`** | `client/src/components/SummaryMetricCard.jsx` | Metric card with icon, key value, subtitle label, optional status badge, and optional inline `ProgressBar`. |
| **`SearchFilterBar`** | `client/src/components/SearchFilterBar.jsx` | Top control bar containing search input, custom filter slot children, total item counter (`Showing X-Y of Z`), and Reset button. |
| **`DataTable`** | `client/src/components/DataTable.jsx` | Reusable tabular viewer with custom column renders, loading spinner overlay, empty state fallback, and pagination controls. |
| **`Modal`** | `client/src/components/Modal.jsx` | Accessible dialog backdrop and container with ESC key handling, backdrop click dismiss, title header, and scrollable body. |
| **`ConfirmDialog`** | `client/src/components/ConfirmDialog.jsx` | Confirmation modal for destructive actions (e.g., deactivating hotels/rooms or deleting accounts) with danger styling variant. |
| **`EmptyState`** | `client/src/components/EmptyState.jsx` | Centered placeholder view with icon, title, description, and action button for zero-data conditions. |
| **`Button`** | `client/src/components/Button.jsx` | Standard button supporting `primary`, `secondary`, `outline`, `danger` variants and pill styling. |
| **`Input`** | `client/src/components/Input.jsx` | Form text input with label, error message, and focus border animations. |
| **`LoadingSpinner`** | `client/src/components/LoadingSpinner.jsx` | Animated spinner with `sm`, `md`, `lg` sizing. |
| **`ErrorBanner`** | `client/src/components/ErrorBanner.jsx` | Dismissible red banner for displaying API error messages. |

---

## 3. Routing & Authorization Structure

All hotel owner routes are defined in `client/src/routes/AppRoutes.jsx` and wrapped inside `<ProtectedRoute allowedRoles={['HotelOwner']} />`:

```jsx
<Route element={<ProtectedRoute allowedRoles={['HotelOwner']} />}>
  <Route path="/hotel-owner" element={<Navigate to="/hotel-owner/hotels" replace />} />
  <Route path="/hotel-owner/hotels" element={<MyHotelsPage />} />
  <Route path="/hotel-owner/hotels/:id" element={<HotelDetailPage />} />
  <Route path="/hotel-owner/profile" element={<ProfilePage />} />
</Route>
```

### Authentication Context (`client/src/context/AuthContext.jsx`)
- Handles JWT storage in `localStorage`.
- Extracts user state from backend `ApiResponse<T>` unwrapping (`res.data.data.user`).
- Exposes `user`, `role`, `login()`, and `logout()`.

---

## 4. Feature Modules & Page Breakdown (`client/src/features/hotel-owner/`)

### 4.1 My Hotels Overview Page (`pages/MyHotelsPage.jsx`)
- **Primary Function**: Overview dashboard listing all hotels owned by the logged-in user.
- **Key Features**:
  1. **Metric Cards**:
     - *Total Hotels* count
     - *Active Hotels* count
     - *Pending Approval* count
     - *Average Occupancy* percentage across all properties
  2. **Search & Status Filter**: Client-side filtering by property name and status (`All`, `Active`, `PendingApproval`, `Suspended`, `Rejected`, `Inactive`).
  3. **Grid Layout**: Displays hotel cards with image, star rating, status badge, occupancy bar, and action buttons (`Edit`, `Deactivate`).
  4. **Modals**: Triggers `AddEditHotelModal` for creating/updating properties and `ConfirmDialog` for deactivation.

---

### 4.2 Hotel Detail Page (`pages/HotelDetailPage.jsx`)
- **Primary Function**: Detailed management view for a specific hotel (`/hotel-owner/hotels/:id`).
- **Key Features**:
  1. **Master Card**: Displays hotel image, address, contact phone, star rating, description, creation date, and overall occupancy rate.
  2. **Room Types Management Table**:
     - Lists all room types (`roomType`, `pricePerNight`, `capacity`, `totalRooms`, `amenities`, `status`).
     - Actions to add a room type (`+ Add Room Type`), edit existing rooms (`RoomModal`), or deactivate room types.
  3. **Recent Bookings Table**:
     - Displays bookings belonging specifically to this hotel (`roomType`, `checkInDate`, `checkOutDate`, `numberOfRooms`, `status`, `totalPrice`).
     - Includes pagination for historical bookings.

---

### 4.3 Owner Profile Settings Page (`pages/ProfilePage.jsx`)
- **Primary Function**: Self-service profile management for authenticated users (`/hotel-owner/profile`).
- **Key Features**:
  1. **Personal Information Form**: Allows updating `FullName` and `Email`.
  2. **Account Metadata**: Displays `Role`, `Account Status`, and `Member Since` date.
  3. **Account Deletion (Danger Zone)**:
     - Checks deletion eligibility via `GET /api/profile/me/deletion-eligibility`.
     - Displays a warning/blocking message if deletion is disallowed (e.g., active hotels or pending bookings exist).
     - Provides a confirm dialog before executing account deletion.

---

### 4.4 Feature Modals & Forms (`components/`)
1. **`AddEditHotelModal.jsx`**:
   - Handles creation (`POST /api/hotels`) and update (`PUT /api/hotels/{id}`).
   - Fetches destination choices dynamically from `GET /api/destinations`.
   - Fields: Name, Destination ID dropdown, Address, Contact Phone, Star Rating (1-5), Description, Image URL.
2. **`RoomModal.jsx`**:
   - Handles room creation (`POST /api/hotels/{id}/rooms`) and update (`PUT /api/hotels/{id}/rooms/{roomId}`).
   - Fields: Room Type (Single, Double, Suite, Deluxe, Family, Penthouse), Price per Night (LKR), Capacity (Guests), Total Rooms Count, Amenities (comma-separated string).

---

## 5. API Integration Layer (`client/src/services/hotelOwnerApi.js`)

All calls automatically unwrap the standard backend `ApiResponse<T>` envelope (`res.data.data`):

```js
// Hotels
getMyHotels({ search, sort, page, pageSize }) // GET /api/hotels/my
getMyHotelDetail(id)                        // GET /api/hotels/my/{id}
createHotel(body)                            // POST /api/hotels
updateHotel(id, body)                        // PUT /api/hotels/{id}
deactivateHotel(id)                          // DELETE /api/hotels/{id}

// Rooms
addRoom(hotelId, body)                       // POST /api/hotels/{hotelId}/rooms
updateRoom(hotelId, roomId, body)            // PUT /api/hotels/{hotelId}/rooms/{roomId}
deactivateRoom(hotelId, roomId)              // DELETE /api/hotels/{hotelId}/rooms/{roomId}

// Bookings
getHotelBookings(hotelId, { page, pageSize }) // GET /api/hotels/{hotelId}/bookings

// Profile
getMyProfile()                               // GET /api/profile/me
updateMyProfile(body)                        // PUT /api/profile/me
getDeletionEligibility()                     // GET /api/profile/me/deletion-eligibility
deleteMyAccount()                            // DELETE /api/profile/me

// Metadata
getDestinations()                            // GET /api/destinations
```

---

## 6. How Future AI & Developers Should Extend This Dashboard

1. **Reusing Shared UI Components**:
   When implementing dashboards for **Transport Provider**, **Supplier**, or **Traveler**, import components directly from `client/src/components/` (`DashboardLayout`, `SummaryMetricCard`, `SearchFilterBar`, `DataTable`, `StatusBadge`, `Modal`, `ConfirmDialog`, `EmptyState`).
2. **Data Structure Standard**:
   Ensure all API handlers unwraps `res.data.data` as the backend sends standard response payloads wrapped in `ApiResponse<T>`.
3. **Routing Pattern**:
   Add new role-based pages in `client/src/routes/AppRoutes.jsx` under their respective `ProtectedRoute` wrappers.
