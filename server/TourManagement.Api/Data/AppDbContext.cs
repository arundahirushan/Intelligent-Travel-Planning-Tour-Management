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
    }
}
