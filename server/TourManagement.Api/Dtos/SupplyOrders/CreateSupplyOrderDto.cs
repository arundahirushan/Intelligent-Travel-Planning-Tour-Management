using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.SupplyOrders;

public class CreateSupplyOrderDto
{
    [Required]
    public int TripId { get; set; }

    [Required]
    public int SupplyId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}
