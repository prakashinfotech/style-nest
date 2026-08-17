using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Wallet;
using StyleNest.Infrastructure.Persistence;
using StyleNest.SharedKernel.DTOs;

namespace StyleNest.User.API.Services;

public class WalletService(AppDbContext db) : IWalletService
{
    public async Task<WalletDto> GetWalletAsync(Guid userId)
    {
        var wallet = await GetOrCreateWalletAsync(userId);
        return Map(wallet);
    }

    public async Task<WalletDto> AddMoneyAsync(Guid userId, decimal amount, string description)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        var wallet = await GetOrCreateWalletAsync(userId);
        wallet.Balance += amount;

        db.WalletTransactions.Add(new WalletTransaction
        {
            Id           = Guid.NewGuid(),
            WalletId     = wallet.Id,
            Amount       = amount,
            Type         = TransactionType.Credit,
            Source       = TransactionSource.ManualTopup,
            Description  = description,
            BalanceAfter = wallet.Balance,
        });

        await db.SaveChangesAsync();
        return Map(wallet);
    }

    public async Task<PagedResult<WalletTransactionDto>> GetTransactionsAsync(Guid userId, int page, int pageSize)
    {
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet is null)
            return new PagedResult<WalletTransactionDto>([], 0, page, pageSize);

        var query = db.WalletTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<WalletTransactionDto>(
            items.Select(t => new WalletTransactionDto(
                t.Id, t.Amount, t.Type.ToString(), t.Source.ToString(),
                t.Description, t.Reference, t.BalanceAfter, t.CreatedAt)).ToList(),
            total, page, pageSize);
    }

    private async Task<Wallet> GetOrCreateWalletAsync(Guid userId)
    {
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet is not null) return wallet;

        wallet = new Wallet { Id = Guid.NewGuid(), UserId = userId, Balance = 0m, Currency = "INR" };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();
        return wallet;
    }

    private static WalletDto Map(Wallet w) => new(w.Id, w.Balance, w.Currency, w.UpdatedAt);
}
