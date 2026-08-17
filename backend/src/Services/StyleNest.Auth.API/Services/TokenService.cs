using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using StyleNest.Infrastructure.Entities.Auth;

namespace StyleNest.Auth.API.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly SigningCredentials _signingCredentials;

    public TokenService(IConfiguration config)
    {
        _config = config;

        var rsa = RSA.Create();
        var privateKeyPem = config["Jwt:PrivateKey"]
            ?? throw new InvalidOperationException("Jwt:PrivateKey not configured.");
        rsa.ImportFromPem(privateKeyPem);
        _signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
    }

    public DateTime AccessTokenExpiresAt =>
        DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:AccessTokenMinutes"] ?? "15"));

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles, Guid? sellerId = null, bool mfaVerified = false)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (sellerId.HasValue)
            claims.Add(new Claim("sellerId", sellerId.Value.ToString()));

        // ENH-AUTH-012: MFA verification claim — required by Admin.API RequireMfa policy
        if (mfaVerified)
            claims.Add(new Claim("mfa_verified", "true"));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: AccessTokenExpiresAt,
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
