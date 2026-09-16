namespace TourManagement.Api.Common;

// Wraps a list of items with pagination metadata.
// Used by any endpoint that returns a paged list (users, trips, destinations).
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }  // total number of matching rows in the DB
    public int Page { get; set; }        // current page number (1-based)
    public int PageSize { get; set; }    // how many items per page
}
