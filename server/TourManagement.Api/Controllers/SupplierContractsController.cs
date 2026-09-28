using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Supplier;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/contracts/my")]
[Authorize(Roles = Roles.Supplier)]
public class SupplierContractsController : ControllerBase
{
    private readonly IContractService _contractService;

    public SupplierContractsController(IContractService contractService)
    {
        _contractService = contractService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found in token.");
        return int.Parse(claim);
    }

    /// <summary>Get contract status summary for the logged-in supplier.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<SupplierContractStatusDto>>> GetMyStatus()
    {
        var result = await _contractService.GetContractStatusSummaryAsync(GetCurrentUserId());
        return Ok(ApiResponse<SupplierContractStatusDto>.Ok(result));
    }

    /// <summary>Get contract history for the logged-in supplier.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ContractSummaryDto>>>> GetMyContracts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _contractService.GetAllContractsAsync(null, GetCurrentUserId(), "enddate_desc", page, pageSize);
        return Ok(ApiResponse<PagedResult<ContractSummaryDto>>.Ok(result));
    }
}
