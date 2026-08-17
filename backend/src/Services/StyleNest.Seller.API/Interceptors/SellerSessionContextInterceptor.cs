using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StyleNest.Seller.API.Services;

namespace StyleNest.Seller.API.Interceptors;

/// <summary>
/// ENH-SELL-001 — Sets SESSION_CONTEXT(N'SellerId') on every new SQL Server connection so that
/// the RLS predicate function (seller.fn_SellerInventoryAccessPredicate) automatically scopes
/// SellerInventory and SellerPayout queries to the authenticated seller.
/// Silently skips non-SQL-Server connections (in-memory, SQLite, etc.).
/// </summary>
public sealed class SellerSessionContextInterceptor(ICurrentSellerContext sellerContext) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetSessionContext(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetSessionContextAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void SetSessionContext(DbConnection connection)
    {
        var sellerId = sellerContext.SellerId;
        if (sellerId is null) return;
        if (!IsSqlServer(connection)) return;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = BuildSetContextSql();
        AddSellerIdParameter(cmd, sellerId.Value);
        cmd.ExecuteNonQuery();
    }

    private async Task SetSessionContextAsync(DbConnection connection, CancellationToken ct)
    {
        var sellerId = sellerContext.SellerId;
        if (sellerId is null) return;
        if (!IsSqlServer(connection)) return;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = BuildSetContextSql();
        AddSellerIdParameter(cmd, sellerId.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static bool IsSqlServer(DbConnection connection)
        => connection.GetType().FullName?.Contains("SqlConnection", StringComparison.OrdinalIgnoreCase) == true;

    private static string BuildSetContextSql()
        => "EXEC sp_set_session_context N'SellerId', @SellerId, @read_only = 1;";

    private static void AddSellerIdParameter(DbCommand cmd, Guid sellerId)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = "@SellerId";
        p.Value         = sellerId;
        cmd.Parameters.Add(p);
    }
}
