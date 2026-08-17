using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace StyleNest.Auth.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route(".well-known")]
public sealed class JwksController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("jwks")]
    [ResponseCache(Duration = 900)] // 15 min — matches JwksKeyProvider cache TTL
    public IActionResult GetJwks()
    {
        var pem = configuration["Jwt:PublicKey"]
            ?? throw new InvalidOperationException("Jwt:PublicKey not configured.");

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        var p = rsa.ExportParameters(includePrivateParameters: false);

        var jwk = new JsonWebKey
        {
            Kty = JsonWebAlgorithmsKeyTypes.RSA,
            Use = "sig",
            Alg = SecurityAlgorithms.RsaSha256,
            N   = Base64UrlEncoder.Encode(p.Modulus!),
            E   = Base64UrlEncoder.Encode(p.Exponent!)
        };

        return Ok(new { keys = new[] { jwk } });
    }
}
