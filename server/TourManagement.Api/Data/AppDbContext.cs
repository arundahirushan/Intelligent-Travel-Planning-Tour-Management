using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Models;

namespace TourManagement.Api.Data;

// The EF Core database context. All database access goes through this class.
// Configuration (indexes, relationships, column types) is in OnModelCreating below.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // One DbSet per entity — EF Core maps each to a database table.
    public DbSet<User> Users => Set<User>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<HotelBooking> HotelBookings => Set<HotelBooking>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleBooking> VehicleBookings => Set<VehicleBooking>();
    public DbSet<Supply> Supplies => Set<Supply>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractRequest> ContractRequests => Set<ContractRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ─────────────────────────────────────────────────────────────

        modelBuilder.Entity<User>(entity =>
        {
            // No two users can share the same email address.
            entity.HasIndex(u => u.Email).IsUnique();

            // Store the enum as its name ("Active", "PendingApproval" etc.)
            // instead of an integer, so the database table is human-readable.
            entity.Property(u => u.Status)
                  .HasConversion<string>();

            // One user can have many trips. If the user is deleted,
            // cascade-delete all their trips (and those trips' itinerary items
            // in turn, because of the Trip→ItineraryItems cascade below).
            entity.HasMany(u => u.Trips)
                  .WithOne(t => t.Traveler)
                  .HasForeignKey(t => t.TravelerId)
                  .OnDelete(DeleteBehavior.Cascade);

            // A HotelOwner can have many hotels. We RESTRICT deletion here:
            // deleting an owner account with hotels would silently lose booking
            // history. The service layer must handle this explicitly.
            entity.HasMany(u => u.Hotels)
                  .WithOne(h => h.Owner)
                  .HasForeignKey(h => h.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // A TransportProvider can have many vehicles. Same reasoning as above:
            // deleting a provider with vehicles would silently lose booking history.
            entity.HasMany(u => u.Vehicles)
                  .WithOne(v => v.Provider)
                  .HasForeignKey(v => v.ProviderId)
                  .OnDelete(DeleteBehavior.Restrict);

            // A Supplier can have many supplies. RESTRICT deletion:
            // deleting a supplier with supplies would break inventory records.
            entity.HasMany(u => u.Supplies)
                  .WithOne(s => s.Supplier)
                  .HasForeignKey(s => s.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);

            // A Supplier can have many contracts. RESTRICT deletion to preserve legal/audit history.
            entity.HasMany(u => u.Contracts)
                  .WithOne(c => c.Supplier)
                  .HasForeignKey(c => c.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);

            // A Supplier can have many contract requests. RESTRICT deletion to preserve audit history.
            entity.HasMany(u => u.ContractRequests)
                  .WithOne(cr => cr.Supplier)
                  .HasForeignKey(cr => cr.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Destination ──────────────────────────────────────────────────────

        modelBuilder.Entity<Destination>(entity =>
        {
            // Destinations are searched by name frequently, so index it.
            entity.HasIndex(d => d.Name);
        });

        // ── Trip ─────────────────────────────────────────────────────────────

        modelBuilder.Entity<Trip>(entity =>
        {
            // Store TripStatus as its name, same reason as UserStatus above.
            entity.Property(t => t.Status)
                  .HasConversion<string>();

            // Budget is money — store with 2 decimal places.
            entity.Property(t => t.Budget)
                  .HasColumnType("decimal(18,2)");

            // These columns are used in WHERE clauses constantly, so index them.
            entity.HasIndex(t => t.TravelerId);
            entity.HasIndex(t => t.Status);

            // If a trip is deleted, remove all its itinerary items automatically.
            // They have no meaning without the trip.
            entity.HasMany(t => t.ItineraryItems)
                  .WithOne(i => i.Trip)
                  .HasForeignKey(i => i.TripId)
                  .OnDelete(DeleteBehavior.Cascade);

            // RESTRICT trip deletion if it has hotel bookings — deleting a trip that
            // has bookings would break hotel booking history.
            // The service layer should handle this with a clear error message.
            entity.HasMany(t => t.HotelBookings)
                  .WithOne(b => b.Trip)
                  .HasForeignKey(b => b.TripId)
                  .OnDelete(DeleteBehavior.Restrict);

            // RESTRICT trip deletion if it has vehicle bookings — same reasoning as above.
            // Consistent with the Trip → Bookings relationship for hotels.
            entity.HasMany(t => t.VehicleBookings)
                  .WithOne(b => b.Trip)
                  .HasForeignKey(b => b.TripId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ItineraryItem ────────────────────────────────────────────────────

        modelBuilder.Entity<ItineraryItem>(entity =>
        {
            // Do NOT allow deleting a Destination that is still referenced by
            // an ItineraryItem — that would silently break existing trip plans.
            // The service layer catches the FK violation and returns a clear error.
            entity.HasOne(i => i.Destination)
                  .WithMany(d => d.ItineraryItems)
                  .HasForeignKey(i => i.DestinationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Hotel ─────────────────────────────────────────────────────────────

        modelBuilder.Entity<Hotel>(entity =>
        {
            // Store HotelStatus as a string for readability.
            entity.Property(h => h.Status)
                  .HasConversion<string>();

            // These are used in WHERE clauses for filtering/search.
            entity.HasIndex(h => h.DestinationId);
            entity.HasIndex(h => h.Status);

            // Rooms have no meaning without their hotel — cascade delete them.
            entity.HasMany(h => h.Rooms)
                  .WithOne(r => r.Hotel)
                  .HasForeignKey(r => r.HotelId)
                  .OnDelete(DeleteBehavior.Cascade);

            // A Hotel belongs to a Destination. RESTRICT deletion of a Destination
            // that still has hotels pointing to it.
            entity.HasOne(h => h.Destination)
                  .WithMany(d => d.Hotels)
                  .HasForeignKey(h => h.DestinationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Room ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<Room>(entity =>
        {
            // Store RoomStatus as a string.
            entity.Property(r => r.Status)
                  .HasConversion<string>();

            // PricePerNight is money — store with 2 decimal places.
            entity.Property(r => r.PricePerNight)
                  .HasColumnType("decimal(18,2)");

            // Indexed because the search query filters by HotelId.
            entity.HasIndex(r => r.HotelId);

            // RESTRICT: don't allow deleting a room that has existing bookings.
            // The booking history must be preserved.
            entity.HasMany(r => r.Bookings)
                  .WithOne(b => b.Room)
                  .HasForeignKey(b => b.RoomId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── HotelBooking ──────────────────────────────────────────────────────

        modelBuilder.Entity<HotelBooking>(entity =>
        {
            // Map to the HotelBookings table (renamed from Bookings).
            entity.ToTable("HotelBookings");

            // Store BookingStatus as a string.
            entity.Property(b => b.Status)
                  .HasConversion<string>();

            // All three columns are frequently used in WHERE clauses.
            entity.HasIndex(b => b.RoomId);
            entity.HasIndex(b => b.TripId);
            entity.HasIndex(b => b.Status);
        });

        // ── Vehicle ───────────────────────────────────────────────────────────

        modelBuilder.Entity<Vehicle>(entity =>
        {
            // Store VehicleStatus as a string for readability.
            entity.Property(v => v.Status)
                  .HasConversion<string>();

            // PricePerDay is money — store with 2 decimal places.
            entity.Property(v => v.PricePerDay)
                  .HasColumnType("decimal(18,2)");

            // Registration numbers must be globally unique.
            entity.HasIndex(v => v.RegistrationNumber).IsUnique();

            // Status is used in WHERE clauses for search and admin filtering.
            entity.HasIndex(v => v.Status);

            // RESTRICT: don't allow deleting a vehicle that has existing bookings.
            // The booking history must be preserved.
            entity.HasMany(v => v.VehicleBookings)
                  .WithOne(b => b.Vehicle)
                  .HasForeignKey(b => b.VehicleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── VehicleBooking ────────────────────────────────────────────────────

        modelBuilder.Entity<VehicleBooking>(entity =>
        {
            // Store BookingStatus as a string (uses the shared enum now).
            entity.Property(b => b.Status)
                  .HasConversion<string>();

            // Coordinates: 18 total digits, 7 decimal places gives ~1cm precision.
            // Using the same decimal column type for consistency with other money columns,
            // but with enough decimal places for GPS-accurate coordinates.
            entity.Property(b => b.PickupLatitude)
                  .HasColumnType("decimal(10,7)");
            entity.Property(b => b.PickupLongitude)
                  .HasColumnType("decimal(11,7)");

            // All three columns are frequently used in WHERE clauses.
            entity.HasIndex(b => b.VehicleId);
            entity.HasIndex(b => b.TripId);
            entity.HasIndex(b => b.Status);
        });

        // ── Supply ────────────────────────────────────────────────────────────

        modelBuilder.Entity<Supply>(entity =>
        {
            // Store enums as strings for readability.
            entity.Property(s => s.Status)
                  .HasConversion<string>();

            entity.Property(s => s.RemovalReason)
                  .HasConversion<string>();

            // PricePerUnit is money in LKR — store with 2 decimal places.
            entity.Property(s => s.PricePerUnit)
                  .HasColumnType("decimal(18,2)");

            // Frequent lookup and filtering columns.
            entity.HasIndex(s => s.SupplierId);
            entity.HasIndex(s => s.Status);
        });

        // ── Contract ──────────────────────────────────────────────────────────

        modelBuilder.Entity<Contract>(entity =>
        {
            // Store ContractStatus as string (Active, Terminated).
            entity.Property(c => c.Status)
                  .HasConversion<string>();

            // Frequent lookup and filtering columns.
            entity.HasIndex(c => c.SupplierId);
            entity.HasIndex(c => c.Status);

            // Renewal requests referencing this contract.
            entity.HasMany(c => c.ContractRequests)
                  .WithOne(cr => cr.ExistingContract)
                  .HasForeignKey(cr => cr.ExistingContractId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ContractRequest ───────────────────────────────────────────────────

        modelBuilder.Entity<ContractRequest>(entity =>
        {
            // Store enums as string.
            entity.Property(cr => cr.RequestType)
                  .HasConversion<string>();

            entity.Property(cr => cr.Status)
                  .HasConversion<string>();

            // Frequent lookup and filtering columns.
            entity.HasIndex(cr => cr.SupplierId);
            entity.HasIndex(cr => cr.Status);
        });
    }
}
