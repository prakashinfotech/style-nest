using FluentAssertions;
using StyleNest.Catalog.API.DTOs;
using StyleNest.Catalog.API.Validators;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class ProductQueryValidatorTests
{
    private readonly ProductQueryValidator _sut = new();

    [Fact]
    public void Validate_ValidQuery_PassesValidation()
    {
        var query = new ProductQueryDto { Page = 1, PageSize = 24 };

        var result = _sut.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativePage_FailsValidation()
    {
        var query = new ProductQueryDto { Page = 0, PageSize = 24 };

        var result = _sut.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Fact]
    public void Validate_PageSizeOver100_FailsValidation()
    {
        var query = new ProductQueryDto { Page = 1, PageSize = 101 };

        var result = _sut.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }
}
