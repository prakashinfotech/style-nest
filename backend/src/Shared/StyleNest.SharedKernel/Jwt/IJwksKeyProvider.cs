using Microsoft.IdentityModel.Tokens;

namespace StyleNest.SharedKernel.Jwt;

public interface IJwksKeyProvider
{
    IEnumerable<SecurityKey> GetSigningKeys();
}
