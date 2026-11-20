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
        var keyInput = configuration["Jwt:PublicKey"] ?? configuration["Jwt:PublicKeyPath"]
            ?? throw new InvalidOperationException("Jwt:PublicKey not configured.");

        string pem;
        if (keyInput.Contains("-----BEGIN"))
        {
            pem = keyInput;
        }
        else
        {
            var candidates = new[]
            {
                keyInput,
                Path.Combine(AppContext.BaseDirectory, keyInput),
                Path.Combine(Directory.GetCurrentDirectory(), keyInput),
                "/app/keys/public.pem",
                "/app/public.pem",
                "./keys/public.pem",
                "./public.pem",
                "keys/public.pem",
                "public.pem"
            };

            var found = candidates.FirstOrDefault(System.IO.File.Exists);
            if (found != null)
            {
                pem = System.IO.File.ReadAllText(found);
            }
            else
            {
                throw new FileNotFoundException(
                    $"JWT public key file not found. Configured: '{keyInput}'. Checked paths: {string.Join(", ", candidates.Distinct())}");
            }
        }

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
