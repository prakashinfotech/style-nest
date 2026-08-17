/**
 * ENH-PDP-004 — Q&A Section endpoints.
 *
 * GET  api/v1/products/{productId}/questions           → paginated questions list
 * POST api/v1/products/{productId}/questions           → post a question [Authorize]
 * GET  api/v1/questions/{questionId}/answers           → paginated answers
 * POST api/v1/questions/{questionId}/answers           → post an answer [Authorize]
 * POST api/v1/answers/{answerId}/upvote                → increment upvote [Authorize]
 * DELETE api/v1/questions/{questionId}                 → soft-delete question [Admin]
 * DELETE api/v1/answers/{answerId}                     → soft-delete answer [Admin]
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

[ApiController]
public sealed class QnaController(IQnaService qna) : ControllerBase
{
    // ─── Questions ───────────────────────────────────────────────────────────

    /// <summary>ENH-PDP-004 — List questions for a product (paginated, newest-first).</summary>
    [HttpGet("api/v1/products/{productId:guid}/questions")]
    public async Task<IActionResult> GetQuestions(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await qna.GetQuestionsAsync(productId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>ENH-PDP-004 — Post a question about a product.</summary>
    [HttpPost("api/v1/products/{productId:guid}/questions")]
    [Authorize]
    public async Task<IActionResult> PostQuestion(
        Guid productId,
        [FromBody] PostQuestionRequest request,
        CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto = await qna.PostQuestionAsync(productId, userId, request, ct);
        return CreatedAtAction(nameof(GetQuestions), new { productId }, dto);
    }

    // ─── Answers ─────────────────────────────────────────────────────────────

    /// <summary>ENH-PDP-004 — List answers for a question (most-upvoted first).</summary>
    [HttpGet("api/v1/questions/{questionId:guid}/answers")]
    public async Task<IActionResult> GetAnswers(
        Guid questionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await qna.GetAnswersAsync(questionId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>ENH-PDP-004 — Post an answer to a question.</summary>
    [HttpPost("api/v1/questions/{questionId:guid}/answers")]
    [Authorize]
    public async Task<IActionResult> PostAnswer(
        Guid questionId,
        [FromBody] PostAnswerRequest request,
        CancellationToken ct = default)
    {
        var answererId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Determine role tag from JWT role claims
        var role = User.IsInRole("Admin") || User.IsInRole("SuperAdmin") ? "Admin"
                 : User.IsInRole("Seller")                              ? "Seller"
                                                                        : "Shopper";

        try
        {
            var dto = await qna.PostAnswerAsync(questionId, answererId, role, request, ct);
            return CreatedAtAction(nameof(GetAnswers), new { questionId }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ─── Upvote ──────────────────────────────────────────────────────────────

    /// <summary>ENH-PDP-004 — Increment upvote count on an answer.</summary>
    [HttpPost("api/v1/answers/{answerId:guid}/upvote")]
    [Authorize]
    public async Task<IActionResult> UpvoteAnswer(Guid answerId, CancellationToken ct = default)
    {
        try
        {
            var dto = await qna.UpvoteAnswerAsync(answerId, ct);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ─── Admin delete ────────────────────────────────────────────────────────

    /// <summary>ENH-PDP-004 — Soft-delete a question (Admin/SuperAdmin only).</summary>
    [HttpDelete("api/v1/questions/{questionId:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteQuestion(Guid questionId, CancellationToken ct = default)
    {
        try
        {
            await qna.DeleteQuestionAsync(questionId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>ENH-PDP-004 — Soft-delete an answer (Admin/SuperAdmin only).</summary>
    [HttpDelete("api/v1/answers/{answerId:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteAnswer(Guid answerId, CancellationToken ct = default)
    {
        try
        {
            await qna.DeleteAnswerAsync(answerId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
