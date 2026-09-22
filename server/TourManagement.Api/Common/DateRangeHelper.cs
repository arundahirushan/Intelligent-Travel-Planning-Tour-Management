namespace TourManagement.Api.Common;

// Shared helper for the standard date-range overlap check.
//
// Both HotelService and VehicleService use this same formula:
//   Two ranges [startA, endA) and [startB, endB) overlap if startA < endB AND startB < endA.
//
// Note: EF Core LINQ queries (AnyAsync, SumAsync) cannot call static C# methods inside
// lambda expressions — those expressions must be translatable to SQL. So HotelService
// and VehicleService keep the two conditions inline in their LINQ predicates for DB queries.
// This helper is used for pure in-memory checks (e.g., unit tests, future non-EF code).
public static class DateRangeHelper
{
    // Returns true if range [startA, endA) overlaps with range [startB, endB).
    public static bool HasOverlap(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
        => startA < endB && startB < endA;
}
