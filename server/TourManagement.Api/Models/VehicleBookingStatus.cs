namespace TourManagement.Api.Models;

// VehicleBookingStatus has been consolidated into the shared BookingStatus enum.
// Both had identical values (Held, Confirmed, Cancelled) so one enum is sufficient.
// This file is kept to avoid git history loss but the type is no longer defined here.
// Use BookingStatus (Models/BookingStatus.cs) everywhere.
