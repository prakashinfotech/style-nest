# .NET Core Controller, Service & Entity Patterns
# Author: Lead Dev — complete before Phase 2 prompts

## Controller Template

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ProductQueryDto query)
    {
        var result = await _productService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _productService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }
}
```

Rules:
- One controller per resource
- No business logic in controllers — delegate to services
- Attribute routing always
- `ProducesResponseType` on every action for Swagger

## Service Template

```csharp
public interface IProductService
{
    Task<PagedResult<ProductResponseDto>> GetPagedAsync(ProductQueryDto query);
    Task<ProductResponseDto?> GetByIdAsync(Guid id);
}

public class ProductService : IProductService
{
    private readonly IRepository<Product> _repo;
    private readonly IMapper _mapper;

    public ProductService(IRepository<Product> repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
    {
        var product = await _repo.GetByIdAsync(id);
        return product is null ? null : _mapper.Map<ProductResponseDto>(product);
    }
}
```

## Entity Template (BaseEntity)

```csharp
public abstract class BaseEntity<TId>
{
    public TId Id { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class Product : BaseEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    // navigation properties...
}
```

## FluentValidation

```csharp
public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
```

Register in Program.cs:
```csharp
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();
```

## AutoMapper Profile

```csharp
public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductResponseDto>();
        CreateMap<CreateProductRequestDto, Product>();
    }
}
```

## EF Core — DbContext Rules
- Use `HasIndex()` in `OnModelCreating` — never raw SQL
- Global soft-delete filter: `modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted)`
- Audit fields set via `SaveChangesInterceptor`
- Migration naming: `Phase<N>_<Schema>_<Change>`
- Never raw SQL — LINQ only

## Program.cs DI Registration Pattern

```csharp
// Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "StyleNest API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
});
```
