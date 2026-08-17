using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Admin.API.DTOs;
using StyleNest.Admin.API.Services;

namespace StyleNest.Admin.API.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController(
    IAdminService adminService,
    IValidator<CreateSellerRequest> createSellerValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminUserDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var users = await adminService.GetAdminUsersAsync(ct);
        return Ok(users);
    }

    [HttpPost("create-seller")]
    [ProducesResponseType<CreateSellerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSeller([FromBody] CreateSellerRequest req, CancellationToken ct)
    {
        var validation = await createSellerValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var (success, error, result) = await adminService.CreateSellerAsync(req, ct);

        if (!success)
        {
            if (error.Contains("already exists"))
                return Conflict(new { message = error });
            return BadRequest(new { message = error });
        }

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
