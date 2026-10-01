# Integration Guide for TourManagement Flutter App

Welcome to the shared Flutter foundation for the Admin/SuperAdmin application.
This branch provides the basic shell, theme, and authentication.

## Project Structure

- `lib/core`: Contains the global theme (`theme.dart`) and configuration (`config.dart`).
- `lib/services`: Contains `api_client.dart` (for HTTP requests) and `auth_service.dart` (for login state).
- `lib/screens`: Shared top-level screens (e.g. `login_screen.dart`, `home_screen.dart`).
- `lib/features`: Your dedicated workspaces.

## Member Ownership Areas (M1-M4)

We have created placeholders for each team member to work in independently:
- **M1:** `lib/features/m1_users_proposals_trips` (Users, AI Proposals & Trips)
  - **Entry:** `M1DashboardScreen`
  - **Includes:** User Account Management (Approvals, Admins), Trip Oversight, and Pending AI Proposal Reviews.
  - **Reuses:** Shared `ApiClient`, `AuthService`, and Theme.
  - **Limitations:** Admin approval doesn't finalize bookings in Flutter; relies on backend endpoints. Dates are rendered as returned by the API without arbitrary local modifications.
- **M2:** `lib/features/m2_accommodation` (Hotels and Accommodation)
  - **Entry:** `M2DashboardScreen`
  - **Includes:** Hotel Approval/Moderation, Hotel/Room Detail Inspection, Booking Oversight.
  - **Reuses:** Shared `ApiClient`, `AuthService`, and Theme.
  - **Limitations:** Booking oversight is explicitly read-only using summary data (`HotelBookingSummaryDto`). No detailed backend booking endpoint is used or added. Moderation actions depend on exact active/pending state.
- **M3:** `lib/features/m3_vehicles_destinations` (Vehicles and Destinations)
- **M4:** `lib/features/m4_users_contracts` (Users and Supplier Contracts)

## How to Integrate Your Feature

1. **Develop within your feature folder.** Create your screens, widgets, and feature-specific services inside your designated `mX` folder.
2. **Use the Shared Theme:** Use `Theme.of(context)` for styling instead of hardcoded colors to maintain a consistent UI.
3. **Use the Shared API Client:** 
   ```dart
   final apiClient = ApiClient();
   final response = await apiClient.get('/your-endpoint');
   ```
   The `ApiClient` automatically handles the `Authorization` header containing the user's JWT token.
4. **Hooking up to the Home Screen:** When your feature's main screen is ready, coordinate with the team to update `lib/screens/home_screen.dart`. Replace the placeholder `SnackBar` with a `Navigator.push` to your actual screen.

## Running and Testing (Physical Device)

To test the application on the physical Samsung test device (`R83YC1NHQXT`), follow this exact process:

1. **Start the backend server:** Ensure the ASP.NET Core API is running at `http://localhost:5160` (e.g. `dotnet run`).
2. **Connect the phone:** Plug in the physical device via USB.
3. **Forward the port:** Run the following ADB command to allow the phone to access the local backend:
   ```bash
   adb -s R83YC1NHQXT reverse tcp:5160 tcp:5160
   ```
   *Note: You may need to repeat this ADB reverse command if you disconnect the USB cable, restart the phone, or if the connection is lost.*
4. **Run Flutter:** Launch the application on the device with the local API base URL:
   ```bash
   C:\src\flutter\bin\flutter.bat run -d R83YC1NHQXT --dart-define=API_BASE_URL=http://127.0.0.1:5160/api
   ```

Please avoid making large architectural changes in `lib/core` or `lib/services` without team consensus. Happy coding!
