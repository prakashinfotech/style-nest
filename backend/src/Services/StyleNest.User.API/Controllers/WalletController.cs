using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StyleNest.User.API.Services;

namespace StyleNest.User.API.Controllers;

[ApiController]
[Route("api/v1/users/me/wallet")]
[Authorize]
public class WalletController(IWalletService walletService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetWallet()
    {
        var wallet = await walletService.GetWalletAsync(UserId);
        return Ok(wallet);
    }

    [HttpPost("add-money")]
    public async Task<IActionResult> AddMoney([FromBody] AddMoneyRequest request)
    {
        var result = await walletService.AddMoneyAsync(UserId, request.Amount, request.Description);
        return Ok(result);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var transactions = await walletService.GetTransactionsAsync(UserId, page, pageSize);
        return Ok(transactions);
    }
}

public record AddMoneyRequest(decimal Amount, string Description = "Manual top-up");
