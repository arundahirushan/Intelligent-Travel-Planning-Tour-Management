namespace TourManagement.Api.Models;

// Represents the stored status of a contract.
// NOTE: Only Active and Terminated are ever stored in the database.
// "Expired" is a live, computed condition (Status == Active AND EndDate < today)
// checked dynamically at query time rather than stored or set by background jobs.
public enum ContractStatus
{
    Active,
    Terminated
}
