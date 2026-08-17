using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StyleNest.Auth.API.DTOs;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Auth.API.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _db;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILockoutService _lockout;
    private readonly IMfaService _mfa;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        AppDbContext db,
        ILogger<AuthService> logger,
        IHttpContextAccessor httpContext,
        ILockoutService lockout,
        IMfaService mfa)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _db = db;
        _logger = logger;
        _httpContext = httpContext;
        _lockout = lockout;
        _mfa = mfa;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
            return Result.Failure<AuthResponseDto>(Error.Conflict("User"));

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            EmailConfirmed = true
        };

        var identityResult = await _userManager.CreateAsync(user, dto.Password);
        if (!identityResult.Succeeded)
        {
            var msg = string.Join("; ", identityResult.Errors.Select(e => e.Description));
            return Result.Failure<AuthResponseDto>(new Error("Identity.Error", msg));
        }

        await _userManager.AddToRoleAsync(user, "Customer");
        _logger.LogInformation("User {Email} registered", user.Email);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Result.Failure<AuthResponseDto>(new Error("Auth.InvalidCredentials", "Invalid email or password."));

        // ENH-AUTH-005: check lockout before attempting password
        if (await _userManager.IsLockedOutAsync(user))
        {
            var remaining = await _lockout.GetRemainingLockoutSecondsAsync(user);
            return Result.Failure<AuthResponseDto>(AuthErrors.AccountLocked(remaining));
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid)
        {
            var lockoutSeconds = await _lockout.RecordFailedAttemptAsync(user, ct);
            if (lockoutSeconds > 0)
                return Result.Failure<AuthResponseDto>(AuthErrors.AccountLocked(lockoutSeconds));

            return Result.Failure<AuthResponseDto>(new Error("Auth.InvalidCredentials", "Invalid email or password."));
        }

        await _lockout.ResetLockoutAsync(user, ct);
        _logger.LogInformation("User {Email} logged in", user.Email);

        // ENH-AUTH-012: Admin / SuperAdmin must complete MFA before receiving a JWT
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
        {
            var mfaToken = await _mfa.BeginAsync(user, ct);
            return Result.Failure<AuthResponseDto>(new Error("Auth.MfaRequired", mfaToken));
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<Result<AuthResponseDto>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, ct);

        if (token is null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            return Result.Failure<AuthResponseDto>(new Error("Auth.InvalidRefreshToken", "Refresh token is invalid or expired."));

        token.IsRevoked = true;
        await _db.SaveChangesAsync(ct);

        return await IssueTokensAsync(token.User, ct);
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, ct);

        if (token is null)
            return Result.Success();

        token.IsRevoked = true;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result<AuthResponseDto>> IssueTokensAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = await _userManager.GetRolesAsync(user);

        Guid? sellerId = null;
        if (roles.Contains("Seller"))
        {
            var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id, ct);
            sellerId = seller?.Id;
        }

        var expiresAt = _tokenService.AccessTokenExpiresAt;
        var accessToken = _tokenService.GenerateAccessToken(user, roles, sellerId);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        var ctx = _httpContext.HttpContext;
        var refreshTokenEntity = new RefreshToken
        {
            Id         = Guid.NewGuid(),
            UserId     = user.Id,
            Token      = rawRefreshToken,
            ExpiresAt  = DateTime.UtcNow.AddDays(30),
            DeviceName = ctx?.Request.Headers["User-Agent"].FirstOrDefault(),
            IpAddress  = ctx?.Connection.RemoteIpAddress?.ToString(),
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new AuthResponseDto(
            accessToken,
            rawRefreshToken,
            expiresAt,
            new UserDto(user.Id, user.Email!, user.FirstName, user.LastName)));
    }
}
