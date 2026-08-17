using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Auth.API.DTOs;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Entities.Seller;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Auth.API.Controllers;

[ApiController]
[Route("api/v1/auth/admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminAuthController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    AppDbContext db) : ControllerBase
{
    [HttpPost("create-admin")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Conflict(new { Message = "Email already in use." });

        var user = new ApplicationUser
        {
            Id             = Guid.NewGuid(),
            UserName       = request.Email,
            Email          = request.Email,
            FirstName      = request.FirstName,
            LastName       = request.LastName,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));

        await userManager.AddToRoleAsync(user, "Admin");

        return Ok(new CreateUserResponse(user.Id, user.Email!, "Admin", user.CreatedAt));
    }

    [HttpPost("create-seller")]
    public async Task<IActionResult> CreateSeller([FromBody] CreateSellerRequest request)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Conflict(new { Message = "Email already in use." });

        var user = new ApplicationUser
        {
            Id             = Guid.NewGuid(),
            UserName       = request.Email,
            Email          = request.Email,
            FirstName      = request.FirstName,
            LastName       = request.LastName,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        if (!await roleManager.RoleExistsAsync("Seller"))
            await roleManager.CreateAsync(new IdentityRole<Guid>("Seller"));

        await userManager.AddToRoleAsync(user, "Seller");

        // Create seller profile
        var slug = request.StoreName
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "");

        db.Sellers.Add(new Seller
        {
            Id          = Guid.NewGuid(),
            UserId      = user.Id,
            StoreName   = request.StoreName,
            Slug        = $"{slug}-{user.Id.ToString()[..8]}",
            Description = $"Welcome to {request.StoreName}",
            Status      = SellerStatus.Pending,
        });

        await db.SaveChangesAsync();

        return Ok(new CreateUserResponse(user.Id, user.Email!, "Seller", user.CreatedAt));
    }
}
