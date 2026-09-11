// Data/AppDbContext.cs — placeholder
// Will be the EF Core DbContext for the application.
// DbSets for domain entities (Trip, Accommodation, Transport, etc.)
// and WorkflowExecution tables (to persist agent workflow state/results)
// will be added in a later prompt when the database schema is defined.

namespace TourManagement.Api.Data;

// Placeholder class — EF Core not wired up yet.
// When Microsoft.EntityFrameworkCore is configured in Program.cs,
// replace this with: public class AppDbContext : DbContext { ... }
public class AppDbContext
{
    // DbSets and OnModelCreating will be added in a later prompt.
}
