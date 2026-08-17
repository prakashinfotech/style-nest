/**
 * ENH-SRCH-003 — Search Synonyms Dictionary: SearchSynonymService tests
 *
 * Acceptance criteria (FR-SRCH, TSD §7.1):
 *   TC-SRCH-003-01: GetAllAsync — returns empty list when no synonyms exist
 *   TC-SRCH-003-02: UpsertAsync — creates new synonym entry with correct fields
 *   TC-SRCH-003-03: UpsertAsync — normalises term to lowercase+trimmed
 *   TC-SRCH-003-04: UpsertAsync — updates existing synonym entry by term
 *   TC-SRCH-003-05: UpsertAsync — throws ArgumentException on empty Term
 *   TC-SRCH-003-06: UpsertAsync — throws ArgumentException on empty Synonyms array
 *   TC-SRCH-003-07: DeleteAsync — soft-deletes synonym entry (IsDeleted = true)
 *   TC-SRCH-003-08: DeleteAsync — idempotent when entry not found
 *   TC-SRCH-003-09: GetAllAsync — returns only active (non-deleted) entries
 *   TC-SRCH-003-10: ExpandAsync — returns synonyms for known term
 *   TC-SRCH-003-11: ExpandAsync — returns empty list for unknown term
 *   TC-SRCH-003-12: ExpandAsync — normalises query term before lookup (case-insensitive)
 *   TC-SRCH-003-13: UpsertAsync — revives soft-deleted synonym entry
 *   TC-SRCH-003-14: GetAllAsync — returns entries ordered alphabetically by term
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Admin.API.Services;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Admin.Tests;

public sealed class SearchSynonymServiceTests : IDisposable
{
    private readonly AppDbContext        _db;
    private readonly SearchSynonymService _svc;

    public SearchSynonymServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _svc = new SearchSynonymService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── GetAllAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-SRCH-003-01: GetAllAsync — empty when no synonyms exist")]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        var result = await _svc.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "TC-SRCH-003-09: GetAllAsync — returns only active entries")]
    public async Task GetAll_IgnoresSoftDeleted()
    {
        var dto = await _svc.UpsertAsync(new UpsertSynonymRequest("tee", ["t-shirt"]));
        await _svc.DeleteAsync(dto.Id);
        await _svc.UpsertAsync(new UpsertSynonymRequest("sneaker", ["trainer", "shoe"]));

        var result = await _svc.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Term.Should().Be("sneaker");
    }

    [Fact(DisplayName = "TC-SRCH-003-14: GetAllAsync — ordered alphabetically by term")]
    public async Task GetAll_OrderedAlphabetically()
    {
        await _svc.UpsertAsync(new UpsertSynonymRequest("trouser", ["pants"]));
        await _svc.UpsertAsync(new UpsertSynonymRequest("blazer",  ["jacket"]));
        await _svc.UpsertAsync(new UpsertSynonymRequest("kurti",   ["kurta", "tunic"]));

        var result = await _svc.GetAllAsync();

        result.Select(r => r.Term).Should().BeInAscendingOrder();
        result[0].Term.Should().Be("blazer");
        result[1].Term.Should().Be("kurti");
        result[2].Term.Should().Be("trouser");
    }

    // ─── UpsertAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-SRCH-003-02: UpsertAsync — creates new entry with correct fields")]
    public async Task Upsert_Creates_CorrectFields()
    {
        var dto = await _svc.UpsertAsync(
            new UpsertSynonymRequest("tee", ["t-shirt", "polo", "top"]));

        dto.Id.Should().NotBeEmpty();
        dto.Term.Should().Be("tee");
        dto.Synonyms.Should().BeEquivalentTo(["t-shirt", "polo", "top"]);
        dto.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "TC-SRCH-003-03: UpsertAsync — normalises term to lowercase+trimmed")]
    public async Task Upsert_NormalisesTerm()
    {
        var dto = await _svc.UpsertAsync(
            new UpsertSynonymRequest("  TEE  ", ["t-shirt"]));

        dto.Term.Should().Be("tee"); // lowercase, trimmed

        var inDb = await _db.SearchSynonyms.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Term == "tee");
        inDb.Should().NotBeNull();
    }

    [Fact(DisplayName = "TC-SRCH-003-04: UpsertAsync — updates existing entry by term")]
    public async Task Upsert_Updates_Existing()
    {
        var first  = await _svc.UpsertAsync(new UpsertSynonymRequest("sneaker", ["trainer"]));
        var second = await _svc.UpsertAsync(new UpsertSynonymRequest("sneaker", ["trainer", "shoe", "kicks"]));

        second.Id.Should().Be(first.Id);           // same row
        second.Synonyms.Should().HaveCount(3);
        second.Synonyms.Should().Contain("kicks");
    }

    [Fact(DisplayName = "TC-SRCH-003-05: UpsertAsync — throws on empty Term")]
    public async Task Upsert_ThrowsOnEmptyTerm()
    {
        var act = async () => await _svc.UpsertAsync(new UpsertSynonymRequest("   ", ["synonym"]));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Term*");
    }

    [Fact(DisplayName = "TC-SRCH-003-06: UpsertAsync — throws on empty Synonyms array")]
    public async Task Upsert_ThrowsOnEmptySynonyms()
    {
        var act = async () => await _svc.UpsertAsync(new UpsertSynonymRequest("tee", []));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Synonyms*");
    }

    [Fact(DisplayName = "TC-SRCH-003-13: UpsertAsync — revives soft-deleted entry")]
    public async Task Upsert_RevivesSoftDeleted()
    {
        var created = await _svc.UpsertAsync(new UpsertSynonymRequest("tee", ["t-shirt"]));
        await _svc.DeleteAsync(created.Id);

        // Entry is gone from normal view
        var afterDelete = await _svc.GetAllAsync();
        afterDelete.Should().BeEmpty();

        // Re-upsert revives it
        var revived = await _svc.UpsertAsync(new UpsertSynonymRequest("tee", ["t-shirt", "polo"]));
        revived.Id.Should().Be(created.Id);
        revived.Synonyms.Should().Contain("polo");

        // Now visible again
        var afterRevive = await _svc.GetAllAsync();
        afterRevive.Should().HaveCount(1);
    }

    // ─── DeleteAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-SRCH-003-07: DeleteAsync — soft-deletes entry (IsDeleted = true)")]
    public async Task Delete_SoftDeletes()
    {
        var dto = await _svc.UpsertAsync(new UpsertSynonymRequest("trouser", ["pants"]));
        await _svc.DeleteAsync(dto.Id);

        var inDb = await _db.SearchSynonyms.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == dto.Id);
        inDb!.IsDeleted.Should().BeTrue();
    }

    [Fact(DisplayName = "TC-SRCH-003-08: DeleteAsync — idempotent when not found")]
    public async Task Delete_Idempotent_NoThrow()
    {
        var act = async () => await _svc.DeleteAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    // ─── ExpandAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-SRCH-003-10: ExpandAsync — returns synonyms for known term")]
    public async Task Expand_KnownTerm_ReturnsSynonyms()
    {
        await _svc.UpsertAsync(new UpsertSynonymRequest("tee", ["t-shirt", "polo"]));

        var synonyms = await _svc.ExpandAsync("tee");

        synonyms.Should().BeEquivalentTo(["t-shirt", "polo"]);
    }

    [Fact(DisplayName = "TC-SRCH-003-11: ExpandAsync — returns empty for unknown term")]
    public async Task Expand_UnknownTerm_ReturnsEmpty()
    {
        var synonyms = await _svc.ExpandAsync("nonexistent-term");
        synonyms.Should().BeEmpty();
    }

    [Fact(DisplayName = "TC-SRCH-003-12: ExpandAsync — normalises query term (case-insensitive)")]
    public async Task Expand_NormalisesQueryTerm()
    {
        await _svc.UpsertAsync(new UpsertSynonymRequest("tee", ["t-shirt"]));

        // Query with different casing should still find the entry
        var synonyms = await _svc.ExpandAsync("  TEE  ");

        synonyms.Should().Contain("t-shirt");
    }
}
