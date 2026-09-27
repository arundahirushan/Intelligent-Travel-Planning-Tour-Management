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
- **M1:** `lib/features/m1_proposals_trips` (AI Proposals and Trips)
- **M2:** `lib/features/m2_accommodation` (Hotels and Accommodation)
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

## Running and Testing

1. Open this folder (`mobile/tour_management_mobile`) in Antigravity or VS Code.
2. Ensure you have run `flutter pub get`.
3. Check `lib/core/config.dart` and update `apiBaseUrl` to point to your backend. If you're testing on an emulator, `10.0.2.2` works for `localhost`. If testing on a physical phone, ensure your phone and PC are on the same Wi-Fi and use your PC's IP address (e.g. `http://192.168.x.x:5160/api`).
4. Select your device using `flutter devices` and run with `flutter run`.

Please avoid making large architectural changes in `lib/core` or `lib/services` without team consensus. Happy coding!
