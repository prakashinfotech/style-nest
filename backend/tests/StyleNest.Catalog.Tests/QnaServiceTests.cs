/**
 * ENH-PDP-004 — Q&A Section: QnaService tests
 *
 * Acceptance criteria (FR-PDP-007):
 *   TC-PDP-004-01: PostQuestion — creates persisted question with correct fields
 *   TC-PDP-004-02: PostQuestion — trims whitespace from question text
 *   TC-PDP-004-03: GetQuestions — returns questions for specified product only
 *   TC-PDP-004-04: GetQuestions — paginates correctly (page 2 returns second batch)
 *   TC-PDP-004-05: GetQuestions — AnswerCount reflects non-deleted answers only
 *   TC-PDP-004-06: PostAnswer — creates answer with correct role tag
 *   TC-PDP-004-07: PostAnswer — throws when question not found
 *   TC-PDP-004-08: GetAnswers — orders by UpvoteCount desc then CreatedAt asc
 *   TC-PDP-004-09: UpvoteAnswer — increments UpvoteCount by 1
 *   TC-PDP-004-10: UpvoteAnswer — throws when answer not found
 *   TC-PDP-004-11: DeleteQuestion — soft-deletes question (IsDeleted = true)
 *   TC-PDP-004-12: DeleteQuestion — throws when question not found
 *   TC-PDP-004-13: DeleteAnswer — soft-deletes answer (IsDeleted = true)
 *   TC-PDP-004-14: GetQuestions — excludes soft-deleted questions
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Catalog.API.Services;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class QnaServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly QnaService  _svc;

    private static readonly Guid ProductId1 = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid ProductId2 = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid UserId     = Guid.Parse("AAAAAAAA-0000-0000-0000-000000000001");

    public QnaServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
        _svc = new QnaService(_db);

        SeedProducts();
    }

    public void Dispose() => _db.Dispose();

    // ─── PostQuestion ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-004-01: PostQuestion — creates persisted question with correct fields")]
    public async Task PostQuestion_CreatesPersistedQuestion()
    {
        var dto = await _svc.PostQuestionAsync(ProductId1, UserId,
            new PostQuestionRequest("Is this product machine-washable?"));

        dto.ProductId.Should().Be(ProductId1);
        dto.UserId.Should().Be(UserId);
        dto.QuestionText.Should().Be("Is this product machine-washable?");
        dto.AnswerCount.Should().Be(0);

        var stored = await _db.ProductQuestions.IgnoreQueryFilters().FirstAsync(q => q.Id == dto.Id);
        stored.IsDeleted.Should().BeFalse();
    }

    [Fact(DisplayName = "TC-PDP-004-02: PostQuestion — trims whitespace from question text")]
    public async Task PostQuestion_TrimsWhitespace()
    {
        var dto = await _svc.PostQuestionAsync(ProductId1, UserId,
            new PostQuestionRequest("  Does it run true to size?  "));

        dto.QuestionText.Should().Be("Does it run true to size?");
    }

    // ─── GetQuestions ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-004-03: GetQuestions — returns questions for specified product only")]
    public async Task GetQuestions_FiltersToProduct()
    {
        await _svc.PostQuestionAsync(ProductId1, UserId, new PostQuestionRequest("Q for product 1"));
        await _svc.PostQuestionAsync(ProductId2, UserId, new PostQuestionRequest("Q for product 2"));

        var result = await _svc.GetQuestionsAsync(ProductId1, 1, 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].ProductId.Should().Be(ProductId1);
        result.TotalCount.Should().Be(1);
    }

    [Fact(DisplayName = "TC-PDP-004-04: GetQuestions — paginates correctly")]
    public async Task GetQuestions_PaginatesCorrectly()
    {
        for (int i = 1; i <= 5; i++)
            await _svc.PostQuestionAsync(ProductId1, UserId, new PostQuestionRequest($"Question {i}"));

        var page1 = await _svc.GetQuestionsAsync(ProductId1, 1, 3);
        var page2 = await _svc.GetQuestionsAsync(ProductId1, 2, 3);

        page1.Items.Should().HaveCount(3);
        page2.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(5);
        page2.TotalCount.Should().Be(5);
    }

    [Fact(DisplayName = "TC-PDP-004-05: GetQuestions — AnswerCount reflects non-deleted answers only")]
    public async Task GetQuestions_AnswerCountExcludesDeleted()
    {
        var question = await _svc.PostQuestionAsync(ProductId1, UserId, new PostQuestionRequest("Q?"));

        // 2 answers — 1 will be soft-deleted
        var ans1 = await _svc.PostAnswerAsync(question.Id, UserId, "Shopper", new PostAnswerRequest("Answer 1"));
        var ans2 = await _svc.PostAnswerAsync(question.Id, UserId, "Shopper", new PostAnswerRequest("Answer 2"));
        await _svc.DeleteAnswerAsync(ans2.Id);

        var result = await _svc.GetQuestionsAsync(ProductId1, 1, 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].AnswerCount.Should().Be(1); // only non-deleted
    }

    // ─── PostAnswer ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-004-06: PostAnswer — creates answer with correct role tag")]
    public async Task PostAnswer_CreatesAnswerWithRole()
    {
        var question = await _svc.PostQuestionAsync(ProductId1, UserId, new PostQuestionRequest("Q?"));

        var dto = await _svc.PostAnswerAsync(question.Id, UserId, "Seller", new PostAnswerRequest("Seller answer"));

        dto.AnswererRole.Should().Be("Seller");
        dto.AnswerText.Should().Be("Seller answer");
        dto.UpvoteCount.Should().Be(0);
        dto.QuestionId.Should().Be(question.Id);
    }

    [Fact(DisplayName = "TC-PDP-004-07: PostAnswer — throws when question not found")]
    public async Task PostAnswer_ThrowsWhenQuestionNotFound()
    {
        var act = async () => await _svc.PostAnswerAsync(
            Guid.NewGuid(), UserId, "Shopper", new PostAnswerRequest("answer"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ─── GetAnswers ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-004-08: GetAnswers — orders by UpvoteCount desc then CreatedAt asc")]
    public async Task GetAnswers_OrderedByUpvoteThenDate()
    {
        var question = await _svc.PostQuestionAsync(ProductId1, UserId, new PostQuestionRequest("Q?"));

        var ans1 = await _svc.PostAnswerAsync(question.Id, UserId, "Shopper", new PostAnswerRequest("Low upvote"));
        var ans2 = await _svc.PostAnswerAsync(question.Id, UserId, "Seller",  new PostAnswerRequest("High upvote"));

        // Upvote ans2 twice
        await _svc.UpvoteAnswerAsync(ans2.Id);
        await _svc.UpvoteAnswerAsync(ans2.Id);

        var result = await _svc.GetAnswersAsync(question.Id, 1, 10);

        result.Items.Should().HaveCount(2);
        result.Items[0].Id.Should().Be(ans2.Id); // highest upvotes first
        result.Items[1].Id.Should().Be(ans1.Id);
    }

    // ─── UpvoteAnswer ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-004-09: UpvoteAnswer — increments UpvoteCount by 1")]
    public async Task UpvoteAnswer_IncrementsCount()
    {
        var question = await _svc.PostQuestionAsync(ProductId1, UserId, new PostQuestionRequest("Q?"));
        var answer   = await _svc.PostAnswerAsync(question.Id, UserId, "Shopper", new PostAnswerRequest("A"));

        var dto = await _svc.UpvoteAnswerAsync(answer.Id);

        dto.UpvoteCount.Should().Be(1);

        var dto2 = await _svc.UpvoteAnswerAsync(answer.Id);
        dto2.UpvoteCount.Should().Be(2);
    }

    [Fact(DisplayName = "TC-PDP-004-10: UpvoteAnswer — throws when answer not found")]
    public async Task UpvoteAnswer_ThrowsWhenNotFound()
    {
        var act = async () => await _svc.UpvoteAnswerAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ─── DeleteQuestion ───────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-004-11: DeleteQuestion — soft-deletes question")]
    public async Task DeleteQuestion_SoftDeletes()
    {
        var question = await _svc.PostQuestionAsync(ProductId1, UserId, new PostQuestionRequest("Q?"));

        await _svc.DeleteQuestionAsync(question.Id);

        // Query filter excludes deleted — IgnoreQueryFilters to verify
        var stored = await _db.ProductQuestions.IgnoreQueryFilters().FirstAsync(q => q.Id == question.Id);
        stored.IsDeleted.Should().BeTrue();

        // Normal query returns nothing
        var result = await _svc.GetQuestionsAsync(ProductId1, 1, 10);
        result.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "TC-PDP-004-12: DeleteQuestion — throws when question not found")]
    public async Task DeleteQuestion_ThrowsWhenNotFound()
    {
        var act = async () => await _svc.DeleteQuestionAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ─── DeleteAnswer ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-004-13: DeleteAnswer — soft-deletes answer")]
    public async Task DeleteAnswer_SoftDeletes()
    {
        var question = await _svc.PostQuestionAsync(ProductId1, UserId, new PostQuestionRequest("Q?"));
        var answer   = await _svc.PostAnswerAsync(question.Id, UserId, "Shopper", new PostAnswerRequest("A"));

        await _svc.DeleteAnswerAsync(answer.Id);

        var stored = await _db.ProductAnswers.IgnoreQueryFilters().FirstAsync(a => a.Id == answer.Id);
        stored.IsDeleted.Should().BeTrue();
    }

    [Fact(DisplayName = "TC-PDP-004-14: GetQuestions — excludes soft-deleted questions")]
    public async Task GetQuestions_ExcludesSoftDeleted()
    {
        var q1 = await _svc.PostQuestionAsync(ProductId1, UserId, new PostQuestionRequest("Keep this"));
        var q2 = await _svc.PostQuestionAsync(ProductId1, UserId, new PostQuestionRequest("Delete this"));
        await _svc.DeleteQuestionAsync(q2.Id);

        var result = await _svc.GetQuestionsAsync(ProductId1, 1, 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(q1.Id);
        result.TotalCount.Should().Be(1);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SeedProducts()
    {
        var cat = new Category
        {
            Id   = Guid.Parse("CCCCCCCC-0000-0000-0000-000000000001"),
            Name = "TestCat", Slug = "test-cat",
        };
        var brand = new Brand
        {
            Id   = Guid.Parse("BBBBBBBB-0000-0000-0000-000000000001"),
            Name = "TestBrand", Slug = "test-brand",
        };
        _db.Categories.Add(cat);
        _db.Brands.Add(brand);
        _db.Products.AddRange(
            MakeProduct(ProductId1, "Product One",  cat.Id, brand.Id),
            MakeProduct(ProductId2, "Product Two",  cat.Id, brand.Id));
        _db.SaveChanges();
    }

    private static Product MakeProduct(Guid id, string name, Guid catId, Guid brandId) =>
        new()
        {
            Id          = id,
            Name        = name,
            Slug        = name.ToLowerInvariant().Replace(" ", "-"),
            Description = name,
            BasePrice   = 999m,
            CategoryId  = catId,
            BrandId     = brandId,
            IsActive    = true,
        };
}
