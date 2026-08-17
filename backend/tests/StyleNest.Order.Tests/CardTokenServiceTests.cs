/**
 * ENH-PAY-006 — Razorpay Vault Tokenisation
 * Acceptance criteria tested here:
 *   - SaveTokenAsync: token is persisted with correct fields; no PAN stored
 *   - SaveTokenAsync: first token for a user is automatically set as default
 *   - SaveTokenAsync: subsequent tokens are NOT auto-default
 *   - SaveTokenAsync: duplicate (userId, razorpayTokenId) → throws InvalidOperationException
 *   - SaveTokenAsync: different user — same razorpayTokenId allowed (no cross-user collision)
 *   - GetUserTokensAsync: returns only requesting user's tokens (cross-tenant isolation)
 *   - GetUserTokensAsync: default token is listed first
 *   - GetTokenAsync: returns null for unknown id
 *   - GetTokenAsync: returns null when token belongs to different user
 *   - DeleteTokenAsync: soft-deletes (IsDeleted=true); token invisible after delete
 *   - DeleteTokenAsync: wrong user → throws KeyNotFoundException
 *   - DeleteTokenAsync: deleted default → successor promoted to default
 *   - SetDefaultAsync: clears other IsDefault flags and sets target
 *   - SetDefaultAsync: wrong user → throws KeyNotFoundException
 *   - IsExpired flag: true when ExpiryYear/Month is in the past
 *   - NetworkDisplay: returns human-readable network name
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Payments;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.Services;
using Xunit;

namespace StyleNest.Order.Tests;

public sealed class CardTokenServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CardTokenService _sut;
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();

    public CardTokenServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new CardTokenService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private SaveCardTokenRequest MakeRequest(
        string tokenId   = "token_ABCDEF123456",
        string last4     = "4242",
        CardNetwork net  = CardNetwork.Visa,
        int month        = 12,
        int year         = 2030,
        string? name     = "Test User",
        string? custId   = "cust_XYZ") =>
        new(tokenId, custId, last4, net, month, year, name);

    // ── SaveTokenAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveToken_Persists_CorrectFields()
    {
        var req    = MakeRequest("token_VISA001", "1234", CardNetwork.Visa, 11, 2028, "Alice Smith");
        var result = await _sut.SaveTokenAsync(_userA, req);

        result.RazorpayTokenId.Should().Be("token_VISA001");
        result.Last4.Should().Be("1234");
        result.Network.Should().Be(CardNetwork.Visa);
        result.ExpiryMonth.Should().Be(11);
        result.ExpiryYear.Should().Be(2028);
        result.CardholderName.Should().Be("Alice Smith");
    }

    [Fact]
    public async Task SaveToken_FirstTokenForUser_IsAutoDefault()
    {
        var result = await _sut.SaveTokenAsync(_userA, MakeRequest());

        result.IsDefault.Should().BeTrue(
            because: "the first card saved for a user is automatically set as default");
    }

    [Fact]
    public async Task SaveToken_SecondTokenForUser_IsNotAutoDefault()
    {
        await _sut.SaveTokenAsync(_userA, MakeRequest("token_ONE"));
        var second = await _sut.SaveTokenAsync(_userA, MakeRequest("token_TWO"));

        second.IsDefault.Should().BeFalse(
            because: "only the first card is auto-default; subsequent saves require explicit SetDefault");
    }

    [Fact]
    public async Task SaveToken_DuplicateForSameUser_ThrowsInvalidOperation()
    {
        await _sut.SaveTokenAsync(_userA, MakeRequest("token_DUP"));

        var act = async () => await _sut.SaveTokenAsync(_userA, MakeRequest("token_DUP"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*token_DUP*");
    }

    [Fact]
    public async Task SaveToken_SameRazorpayTokenId_DifferentUser_Allowed()
    {
        await _sut.SaveTokenAsync(_userA, MakeRequest("token_SHARED"));

        // User B is a separate customer — same vault token should be allowed
        var act = async () => await _sut.SaveTokenAsync(_userB, MakeRequest("token_SHARED"));

        await act.Should().NotThrowAsync(
            because: "cross-user duplicate check is scoped to (userId, tokenId); " +
                     "different users may obtain the same Razorpay token independently");
    }

    // ── GetUserTokensAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserTokens_ReturnsOnlyOwningUsersTokens()
    {
        await _sut.SaveTokenAsync(_userA, MakeRequest("token_A1"));
        await _sut.SaveTokenAsync(_userA, MakeRequest("token_A2"));
        await _sut.SaveTokenAsync(_userB, MakeRequest("token_B1"));

        var tokensA = (await _sut.GetUserTokensAsync(_userA)).ToList();
        var tokensB = (await _sut.GetUserTokensAsync(_userB)).ToList();

        tokensA.Should().HaveCount(2, because: "User A has 2 saved tokens");
        tokensB.Should().HaveCount(1, because: "User B has 1 saved token");
    }

    [Fact]
    public async Task GetUserTokens_DefaultTokenIsListedFirst()
    {
        await _sut.SaveTokenAsync(_userA, MakeRequest("token_FIRST"));
        var second = await _sut.SaveTokenAsync(_userA, MakeRequest("token_SECOND"));
        await _sut.SetDefaultAsync(_userA, second.Id);

        var tokens = (await _sut.GetUserTokensAsync(_userA)).ToList();

        tokens[0].Id.Should().Be(second.Id,
            because: "the default card should be listed first in the response");
    }

    // ── GetTokenAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetToken_UnknownId_ReturnsNull()
    {
        var result = await _sut.GetTokenAsync(_userA, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetToken_WrongUser_ReturnsNull()
    {
        var saved = await _sut.SaveTokenAsync(_userA, MakeRequest());

        var result = await _sut.GetTokenAsync(_userB, saved.Id);

        result.Should().BeNull(
            because: "a user must not be able to read another user's card token");
    }

    // ── DeleteTokenAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteToken_SoftDeletes_TokenBecomesInvisible()
    {
        var saved = await _sut.SaveTokenAsync(_userA, MakeRequest());

        await _sut.DeleteTokenAsync(_userA, saved.Id);

        var result = await _sut.GetTokenAsync(_userA, saved.Id);
        result.Should().BeNull(because: "soft-deleted token must not be returned by queries");
    }

    [Fact]
    public async Task DeleteToken_WrongUser_ThrowsKeyNotFound()
    {
        var saved = await _sut.SaveTokenAsync(_userA, MakeRequest());

        var act = async () => await _sut.DeleteTokenAsync(_userB, saved.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteToken_DefaultCard_PromotesSuccessor()
    {
        var first  = await _sut.SaveTokenAsync(_userA, MakeRequest("token_FIRST"));
        var second = await _sut.SaveTokenAsync(_userA, MakeRequest("token_SECOND"));

        // first is default; delete it
        await _sut.DeleteTokenAsync(_userA, first.Id);

        var remaining = (await _sut.GetUserTokensAsync(_userA)).ToList();
        remaining.Should().HaveCount(1);
        remaining[0].Id.Should().Be(second.Id);
        remaining[0].IsDefault.Should().BeTrue(
            because: "when the default card is deleted the next card is promoted");
    }

    // ── SetDefaultAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SetDefault_ClearsOtherDefaultsAndSetsTarget()
    {
        var first  = await _sut.SaveTokenAsync(_userA, MakeRequest("token_F"));
        var second = await _sut.SaveTokenAsync(_userA, MakeRequest("token_S"));

        first.IsDefault.Should().BeTrue("first is auto-default");

        await _sut.SetDefaultAsync(_userA, second.Id);

        var tokens = (await _sut.GetUserTokensAsync(_userA)).ToList();
        tokens.Single(t => t.Id == second.Id).IsDefault.Should().BeTrue();
        tokens.Single(t => t.Id == first.Id).IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task SetDefault_WrongUser_ThrowsKeyNotFound()
    {
        var saved = await _sut.SaveTokenAsync(_userA, MakeRequest());

        var act = async () => await _sut.SetDefaultAsync(_userB, saved.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── IsExpired flag ────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveToken_ExpiredCard_IsExpiredFlagTrue()
    {
        var req = MakeRequest(year: 2020, month: 1); // definitely expired

        var result = await _sut.SaveTokenAsync(_userA, req);

        result.IsExpired.Should().BeTrue(
            because: "the IsExpired flag must be set for cards whose expiry date has passed");
    }

    [Fact]
    public async Task SaveToken_ValidCard_IsExpiredFlagFalse()
    {
        var req = MakeRequest(year: 2035, month: 12); // far future

        var result = await _sut.SaveTokenAsync(_userA, req);

        result.IsExpired.Should().BeFalse();
    }

    // ── Network display ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(CardNetwork.Visa,       "Visa")]
    [InlineData(CardNetwork.Mastercard, "Mastercard")]
    [InlineData(CardNetwork.Amex,       "American Express")]
    [InlineData(CardNetwork.Rupay,      "RuPay")]
    [InlineData(CardNetwork.Unknown,    "Unknown")]
    public async Task SaveToken_NetworkDisplay_ReturnsHumanReadableName(
        CardNetwork network, string expected)
    {
        var req    = MakeRequest(net: network);
        var result = await _sut.SaveTokenAsync(_userA, req);

        result.NetworkDisplay.Should().Be(expected);
    }
}
