using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.SupplyOrders;

public class UpdateSupplyOrderDto
{
    [Required]
    public int SupplyId { get; set; }

    [Required]
    [Range(1, 10000, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}
