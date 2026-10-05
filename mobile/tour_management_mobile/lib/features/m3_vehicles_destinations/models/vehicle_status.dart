// Vehicle status string constants matching the backend VehicleStatus enum.
// The backend stores and serializes enums as strings.
class VehicleStatus {
  static const pendingApproval = 'PendingApproval';
  static const active = 'Active';
  static const rejected = 'Rejected';
  static const suspended = 'Suspended';
  static const inactive = 'Inactive';
}

// Shared BookingStatus string constants matching the backend BookingStatus enum.
class BookingStatus {
  static const held = 'Held';
  static const confirmed = 'Confirmed';
  static const cancelled = 'Cancelled';
}
