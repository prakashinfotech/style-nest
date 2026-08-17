using StyleNest.SharedKernel.DTOs;

namespace StyleNest.User.API.Services;

public record WalletDto(Guid Id, decimal Balance, string Currency, DateTime UpdatedAt);
public record WalletTransactionDto(
    Guid Id, decimal Amount, string Type, string Source,
    string Description, string? Reference, decimal BalanceAfter, DateTime CreatedAt);

public interface IWalletService
{
    Task<WalletDto> GetWalletAsync(Guid userId);
    Task<WalletDto> AddMoneyAsync(Guid userId, decimal amount, string description);
    Task<PagedResult<WalletTransactionDto>> GetTransactionsAsync(Guid userId, int page, int pageSize);
}
