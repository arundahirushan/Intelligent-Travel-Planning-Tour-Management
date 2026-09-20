using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Supplier;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/contracts")]
[Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
public class ContractsController : ControllerBase
{
    private readonly IContractService _contractService;

    public ContractsController(IContractService contractService)
    {
        _contractService = contractService;
    }

    /// <summary>Create the first contract for a supplier. Admin/SuperAdmin only.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ContractDetailDto>>> CreateFirstContract([FromBody] CreateContractDto dto)
    {
        var result = await _contractService.CreateFirstContractAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<ContractDetailDto>.Ok(result, "Contract created successfully."));
    }

    /// <summary>List all contracts with computed status filter (Active, Expired, Terminated), supplier filter, and pagination. Admin/SuperAdmin only.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ContractSummaryDto>>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? supplierId,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _contractService.GetAllContractsAsync(status, supplierId, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<ContractSummaryDto>>.Ok(result));
    }

    /// <summary>Get contract details by ID. Admin/SuperAdmin only.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ContractDetailDto>>> GetById(int id)
    {
        var result = await _contractService.GetByIdAsync(id);
        return Ok(ApiResponse<ContractDetailDto>.Ok(result));
    }

    /// <summary>Terminate an active contract. Admin/SuperAdmin only.</summary>
    [HttpPost("{id}/terminate")]
    public async Task<ActionResult<ApiResponse<ContractDetailDto>>> Terminate(int id)
    {
        var result = await _contractService.TerminateAsync(id);
        return Ok(ApiResponse<ContractDetailDto>.Ok(result, "Contract terminated successfully."));
    }

    /// <summary>Check contract validity and summary for a supplier at a glance. Admin/SuperAdmin only.</summary>
    [HttpGet("/api/suppliers/{supplierId}/contract-status")]
    public async Task<ActionResult<ApiResponse<SupplierContractStatusDto>>> GetSupplierContractStatus(int supplierId)
    {
        var result = await _contractService.GetContractStatusSummaryAsync(supplierId);
        return Ok(ApiResponse<SupplierContractStatusDto>.Ok(result));
    }
}
