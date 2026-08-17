/**
 * ENH-PDP-004 — Q&A Section service.
 *
 * Business rules (FR-PDP-007):
 *   - Any authenticated user may post a question.
 *   - Any authenticated user may post an answer (role tag: Shopper / Seller / Admin).
 *   - Users may upvote any answer once (no per-user de-dup here — simple counter).
 *   - Questions and answers are paginated (default page size 10).
 *   - Admin/SuperAdmin may soft-delete questions or answers.
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using StyleNest.SharedKernel.DTOs;

namespace StyleNest.Catalog.API.Services;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public sealed record PostQuestionRequest(string QuestionText);

public sealed record PostAnswerRequest(string AnswerText);

public sealed record QuestionDto(
    Guid      Id,
    Guid      ProductId,
    Guid      UserId,
    string    QuestionText,
    DateTime  CreatedAt,
    int       AnswerCount);

public sealed record AnswerDto(
    Guid      Id,
    Guid      QuestionId,
    Guid      AnswererId,
    string    AnswererRole,
    string    AnswerText,
    int       UpvoteCount,
    DateTime  CreatedAt);

// ─── Interface ───────────────────────────────────────────────────────────────

public interface IQnaService
{
    Task<QuestionDto>           PostQuestionAsync(Guid productId, Guid userId, PostQuestionRequest req, CancellationToken ct = default);
    Task<PagedResult<QuestionDto>> GetQuestionsAsync(Guid productId, int page, int pageSize, CancellationToken ct = default);
    Task<AnswerDto>             PostAnswerAsync(Guid questionId, Guid answererId, string answererRole, PostAnswerRequest req, CancellationToken ct = default);
    Task<PagedResult<AnswerDto>> GetAnswersAsync(Guid questionId, int page, int pageSize, CancellationToken ct = default);
    Task<AnswerDto>             UpvoteAnswerAsync(Guid answerId, CancellationToken ct = default);
    Task                        DeleteQuestionAsync(Guid questionId, CancellationToken ct = default);
    Task                        DeleteAnswerAsync(Guid answerId, CancellationToken ct = default);
}

// ─── Implementation ──────────────────────────────────────────────────────────

public sealed class QnaService(AppDbContext db) : IQnaService
{
    private const int DefaultPageSize = 10;

    public async Task<QuestionDto> PostQuestionAsync(
        Guid productId, Guid userId, PostQuestionRequest req, CancellationToken ct = default)
    {
        var question = new ProductQuestion
        {
            Id           = Guid.NewGuid(),
            ProductId    = productId,
            UserId       = userId,
            QuestionText = req.QuestionText.Trim(),
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };

        db.ProductQuestions.Add(question);
        await db.SaveChangesAsync(ct);

        return MapQuestion(question, 0);
    }

    public async Task<PagedResult<QuestionDto>> GetQuestionsAsync(
        Guid productId, int page, int pageSize, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.ProductQuestions
            .AsNoTracking()
            .Where(q => q.ProductId == productId)
            .OrderByDescending(q => q.CreatedAt);

        var total = await query.CountAsync(ct);

        var questions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new
            {
                q.Id, q.ProductId, q.UserId, q.QuestionText, q.CreatedAt,
                AnswerCount = q.Answers.Count(a => !a.IsDeleted),
            })
            .ToListAsync(ct);

        var items = questions.Select(q =>
            new QuestionDto(q.Id, q.ProductId, q.UserId, q.QuestionText, q.CreatedAt, q.AnswerCount))
            .ToList();

        return new PagedResult<QuestionDto>(items, total, page, pageSize);
    }

    public async Task<AnswerDto> PostAnswerAsync(
        Guid questionId, Guid answererId, string answererRole, PostAnswerRequest req, CancellationToken ct = default)
    {
        var questionExists = await db.ProductQuestions.AnyAsync(q => q.Id == questionId, ct);
        if (!questionExists)
            throw new InvalidOperationException($"Question {questionId} not found.");

        var answer = new ProductAnswer
        {
            Id           = Guid.NewGuid(),
            QuestionId   = questionId,
            AnswererId   = answererId,
            AnswererRole = answererRole,
            AnswerText   = req.AnswerText.Trim(),
            UpvoteCount  = 0,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };

        db.ProductAnswers.Add(answer);
        await db.SaveChangesAsync(ct);

        return MapAnswer(answer);
    }

    public async Task<PagedResult<AnswerDto>> GetAnswersAsync(
        Guid questionId, int page, int pageSize, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.ProductAnswers
            .AsNoTracking()
            .Where(a => a.QuestionId == questionId)
            .OrderByDescending(a => a.UpvoteCount)
            .ThenBy(a => a.CreatedAt);

        var total = await query.CountAsync(ct);

        var answers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AnswerDto>(answers.Select(MapAnswer).ToList(), total, page, pageSize);
    }

    public async Task<AnswerDto> UpvoteAnswerAsync(Guid answerId, CancellationToken ct = default)
    {
        var answer = await db.ProductAnswers.FindAsync([answerId], ct)
                     ?? throw new InvalidOperationException($"Answer {answerId} not found.");

        answer.UpvoteCount++;
        answer.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return MapAnswer(answer);
    }

    public async Task DeleteQuestionAsync(Guid questionId, CancellationToken ct = default)
    {
        var question = await db.ProductQuestions.FindAsync([questionId], ct)
                       ?? throw new InvalidOperationException($"Question {questionId} not found.");

        question.IsDeleted = true;
        question.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAnswerAsync(Guid answerId, CancellationToken ct = default)
    {
        var answer = await db.ProductAnswers.FindAsync([answerId], ct)
                     ?? throw new InvalidOperationException($"Answer {answerId} not found.");

        answer.IsDeleted = true;
        answer.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ─── private helpers ─────────────────────────────────────────────────────

    private static QuestionDto MapQuestion(ProductQuestion q, int answerCount) =>
        new(q.Id, q.ProductId, q.UserId, q.QuestionText, q.CreatedAt, answerCount);

    private static AnswerDto MapAnswer(ProductAnswer a) =>
        new(a.Id, a.QuestionId, a.AnswererId, a.AnswererRole, a.AnswerText, a.UpvoteCount, a.CreatedAt);
}
