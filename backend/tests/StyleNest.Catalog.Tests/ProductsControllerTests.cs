using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using StyleNest.Catalog.API.Controllers;
using StyleNest.Catalog.API.DTOs;
using StyleNest.Catalog.API.Services;
using StyleNest.SharedKernel.DTOs;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class ProductsControllerTests
{
    private readonly Mock<ICatalogService>              _catalogServiceMock;
    private readonly Mock<IValidator<CreateProductRequest>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateProductRequest>> _updateValidatorMock;
    private readonly Mock<IValidator<CreateReviewRequest>>  _reviewValidatorMock;
    private readonly ProductsController                 _sut;

    public ProductsControllerTests()
    {
        _catalogServiceMock  = new Mock<ICatalogService>();
        _createValidatorMock = new Mock<IValidator<CreateProductRequest>>();
        _updateValidatorMock = new Mock<IValidator<UpdateProductRequest>>();
        _reviewValidatorMock = new Mock<IValidator<CreateReviewRequest>>();

        _sut = new ProductsController(
            _catalogServiceMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object,
            _reviewValidatorMock.Object);
    }

    // ── Helper: build a minimal ProductDto ───────────────────────────────────

    private static ProductDto MakeProductDto(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        "Test Product", "test-product", "Description",
        999m, null,
        Guid.NewGuid(), "Brand",
        Guid.NewGuid(), "Category",
        new List<string>(),
        new List<ProductVariantDto>(),
        new List<ProductAttributeDto>(),
        4.0, 10, true);

    // ── GetProduct ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProduct_Returns200_WhenProductExists()
    {
        var productId = Guid.NewGuid();
        var dto = MakeProductDto(productId);

        _catalogServiceMock
            .Setup(s => s.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _sut.GetProduct(productId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetProduct_Returns404_WhenMissing()
    {
        _catalogServiceMock
            .Setup(s => s.GetProductAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        var result = await _sut.GetProduct(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>()
              .Which.StatusCode.Should().Be(404);
    }

    // ── GetProducts ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_Returns200_WithPagedResult()
    {
        var pagedResult = new PagedResult<ProductDto>(
            new List<ProductDto> { MakeProductDto() }, 1, 1, 24);

        _catalogServiceMock
            .Setup(s => s.GetProductsAsync(It.IsAny<ProductQueryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _sut.GetProducts(new ProductQueryDto { Page = 1, PageSize = 24 }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    // ── PostReview ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostReview_Returns401_WhenUnauthenticated()
    {
        var productId = Guid.NewGuid();
        var dto = MakeProductDto(productId);
        var req = new CreateReviewRequest(5, "Great", "Loved it");

        _reviewValidatorMock
            .Setup(v => v.ValidateAsync(req, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _catalogServiceMock
            .Setup(s => s.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // No user claims — NameIdentifier claim is missing
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()) // no claims
            }
        };

        var result = await _sut.PostReview(productId, req, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>()
              .Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task PostReview_Returns404_WhenProductMissing()
    {
        var productId = Guid.NewGuid();
        var req = new CreateReviewRequest(4, "Good", "Nice product");

        _reviewValidatorMock
            .Setup(v => v.ValidateAsync(req, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _catalogServiceMock
            .Setup(s => s.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }))
            }
        };

        var result = await _sut.PostReview(productId, req, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>()
              .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PostReview_Returns400_WhenValidationFails()
    {
        var productId = Guid.NewGuid();
        var req = new CreateReviewRequest(0, "", ""); // invalid

        var failures = new List<ValidationFailure>
        {
            new("Rating", "Rating must be between 1 and 5"),
            new("Title",  "Title is required"),
        };

        _reviewValidatorMock
            .Setup(v => v.ValidateAsync(req, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var result = await _sut.PostReview(productId, req, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task PostReview_Returns201_WhenValid()
    {
        var productId = Guid.NewGuid();
        var userId    = Guid.NewGuid();
        var req       = new CreateReviewRequest(5, "Excellent", "Really loved this");
        var dto       = MakeProductDto(productId);

        var reviewDto = new ReviewDto(
            Guid.NewGuid(), productId, userId, "Alice", 5, "Excellent", "Really loved this", DateTime.UtcNow);

        _reviewValidatorMock
            .Setup(v => v.ValidateAsync(req, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _catalogServiceMock
            .Setup(s => s.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        _catalogServiceMock
            .Setup(s => s.CreateReviewAsync(productId, userId, "Alice", req, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviewDto);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, "Alice"),
                }))
            }
        };

        var result = await _sut.PostReview(productId, req, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(reviewDto);
    }

    // ── GetRelated ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRelated_Returns200_WithRelatedProducts()
    {
        var productId = Guid.NewGuid();
        var dto = MakeProductDto(productId);
        var related = new List<ProductDto> { MakeProductDto(), MakeProductDto() };

        _catalogServiceMock
            .Setup(s => s.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        _catalogServiceMock
            .Setup(s => s.GetRelatedProductsAsync(productId, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(related);

        var result = await _sut.GetRelated(productId, limit: 6, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetRelated_Returns404_WhenProductMissing()
    {
        _catalogServiceMock
            .Setup(s => s.GetProductAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        var result = await _sut.GetRelated(Guid.NewGuid(), limit: 6, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>()
              .Which.StatusCode.Should().Be(404);
    }
}
