using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Admin;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Entities.Seller;

namespace StyleNest.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        await db.Database.MigrateAsync();
        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager, db);
        await SeedCatalogAsync(db);
        await SeedAdminContentAsync(db);
    }

    // ── Roles ─────────────────────────────────────────────────────────────────

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in new[] { "SuperAdmin", "Admin", "Seller", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        // SuperAdmin
        await EnsureUserAsync(userManager, "superadmin@mailinator.com", "Test@123",
            "Super", "Admin", "SuperAdmin");

        // Admins
        for (int i = 1; i <= 4; i++)
            await EnsureUserAsync(userManager, $"admin{i}@mailinator.com", "Test@123",
                $"Admin{i}", "StyleNest", "Admin");

        // Sellers (with Seller profile)
        for (int i = 1; i <= 20; i++)
        {
            var email = $"seller{i:D2}@mailinator.com";
            var userId = await EnsureUserAsync(userManager, email, "Test@123",
                $"Seller{i:D2}", "Store", "Seller");

            if (userId != Guid.Empty && !await db.Sellers.IgnoreQueryFilters().AnyAsync(s => s.UserId == userId))
            {
                var storeName = SellerStoreNames[i - 1];
                var slug = storeName.ToLowerInvariant().Replace(" ", "-").Replace("'", "");
                db.Sellers.Add(new Seller
                {
                    Id          = Guid.NewGuid(),
                    UserId      = userId,
                    StoreName   = storeName,
                    Slug        = $"{slug}-{userId.ToString()[..8]}",
                    Description = $"Welcome to {storeName} — your premium fashion destination.",
                    Status      = i <= 15 ? SellerStatus.Active : SellerStatus.Pending,
                    CommissionRate = 12m + (i % 5),
                    ApprovedAt  = i <= 15 ? DateTime.UtcNow.AddDays(-i * 10) : null,
                });
            }
        }

        // Customers
        for (int i = 1; i <= 15; i++)
            await EnsureUserAsync(userManager, $"user{i:D2}@mailinator.com", "Test@123",
                $"User{i:D2}", "Customer", "Customer");

        await db.SaveChangesAsync();
    }

    private static async Task<Guid> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string password, string firstName, string lastName, string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return existing.Id;

        var user = new ApplicationUser
        {
            Id             = Guid.NewGuid(),
            UserName       = email,
            Email          = email,
            FirstName      = firstName,
            LastName       = lastName,
            EmailConfirmed = true,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
            return user.Id;
        }

        return Guid.Empty;
    }

    // ── Catalog ───────────────────────────────────────────────────────────────

    private static async Task SeedCatalogAsync(AppDbContext db)
    {
        // Clear products (but keep categories/brands)
        if (await db.Products.IgnoreQueryFilters().AnyAsync())
        {
            await db.CartItems.IgnoreQueryFilters().ExecuteDeleteAsync();
            await db.WishlistItems.IgnoreQueryFilters().ExecuteDeleteAsync();
            await db.OrderItems.IgnoreQueryFilters().ExecuteDeleteAsync();
            await db.ProductAttributes.IgnoreQueryFilters().ExecuteDeleteAsync();
            await db.ProductVariants.IgnoreQueryFilters().ExecuteDeleteAsync();
            await db.ProductImages.IgnoreQueryFilters().ExecuteDeleteAsync();
            await db.Products.IgnoreQueryFilters().ExecuteDeleteAsync();
        }

        // Categories
        var catIds = await SeedCategoriesAsync(db);

        // Brands
        var brandIds = await SeedBrandsAsync(db);

        // Attribute Definitions
        await SeedAttributeDefinitionsAsync(db);

        // Products
        await SeedProductsAsync(db, catIds, brandIds);
    }

    private static async Task<Dictionary<string, Guid>> SeedCategoriesAsync(AppDbContext db)
    {
        var ids = new Dictionary<string, Guid>();
        var existing = await db.Categories.IgnoreQueryFilters().ToListAsync();

        (string slug, string name, string? parentSlug)[] categories =
        [
            ("men",       "Men",          null),
            ("women",     "Women",        null),
            ("kids",      "Kids",         null),
            ("footwear",  "Footwear",     null),
            ("jewellery", "Jewellery",    null),
            ("beauty",    "Beauty",       null),
            ("accessories","Accessories", null),
            ("home",      "Home & Living",null),
            ("men-shirts",  "Shirts",       "men"),
            ("men-tshirts", "T-Shirts",     "men"),
            ("men-jeans",   "Jeans",        "men"),
            ("men-jackets", "Jackets",      "men"),
            ("women-dresses","Dresses",     "women"),
            ("women-tops",  "Tops",         "women"),
            ("women-jeans", "Jeans",        "women"),
            ("women-ethnic","Ethnic Wear",  "women"),
            ("kids-boys",   "Boys",         "kids"),
            ("kids-girls",  "Girls",        "kids"),
        ];

        int order = 0;
        foreach (var (slug, name, parentSlug) in categories)
        {
            var existCat = existing.FirstOrDefault(c => c.Slug == slug);
            if (existCat is not null)
            {
                ids[slug] = existCat.Id;
                continue;
            }

            Guid? parentId = parentSlug is not null && ids.TryGetValue(parentSlug, out var pid) ? pid : null;
            var id = Guid.NewGuid();
            ids[slug] = id;
            db.Categories.Add(new Category
            {
                Id           = id,
                Name         = name,
                Slug         = slug,
                ParentId     = parentId,
                DisplayOrder = order++,
            });
        }

        await db.SaveChangesAsync();
        return ids;
    }

    private static async Task<Dictionary<string, Guid>> SeedBrandsAsync(AppDbContext db)
    {
        var ids = new Dictionary<string, Guid>();
        var existing = await db.Brands.IgnoreQueryFilters().ToListAsync();

        var brands = new[]
        {
            ("nike", "Nike"), ("puma", "Puma"), ("adidas", "Adidas"), ("levis", "Levi's"),
            ("hm", "H&M"), ("tanishq", "Tanishq"), ("caratlane", "CaratLane"), ("zara", "Zara"),
            ("bata", "Bata"), ("lakme", "Lakmé"), ("westside", "Westside"), ("pantaloons", "Pantaloons"),
            ("fabindia", "FabIndia"), ("manyavar", "Manyavar"), ("global-desi", "Global Desi"),
            ("reebok", "Reebok"), ("skechers", "Skechers"), ("woodland", "Woodland"),
            ("raymond", "Raymond"), ("arrow", "Arrow"),
        };

        foreach (var (slug, name) in brands)
        {
            var existBrand = existing.FirstOrDefault(b => b.Slug == slug);
            if (existBrand is not null)
            {
                ids[slug] = existBrand.Id;
                continue;
            }

            var id = Guid.NewGuid();
            ids[slug] = id;
            db.Brands.Add(new Brand { Id = id, Name = name, Slug = slug });
        }

        await db.SaveChangesAsync();
        return ids;
    }

    private static async Task SeedAttributeDefinitionsAsync(AppDbContext db)
    {
        if (await db.AttributeDefinitions.IgnoreQueryFilters().AnyAsync()) return;

        var attrs = new[]
        {
            ("color",    "Color",    "Select",  true,  """["Black","White","Red","Blue","Green","Yellow","Pink","Grey","Navy","Brown"]"""),
            ("size",     "Size",     "Select",  true,  """["XS","S","M","L","XL","XXL","XXXL"]"""),
            ("material", "Material", "Select",  true,  """["Cotton","Polyester","Silk","Wool","Linen","Denim","Leather","Nylon"]"""),
            ("fit",      "Fit",      "Select",  true,  """["Slim Fit","Regular Fit","Relaxed Fit","Oversized","Skinny","Straight"]"""),
            ("occasion", "Occasion", "Select",  true,  """["Casual","Formal","Sports","Party","Wedding","Office","Beach"]"""),
            ("pattern",  "Pattern",  "Select",  false, """["Solid","Striped","Checked","Floral","Printed","Plain","Abstract"]"""),
            ("sleeve",   "Sleeve Type", "Select", false, """["Full Sleeve","Half Sleeve","Sleeveless","3/4 Sleeve"]"""),
            ("neck",     "Neck Type", "Select",  false, """["Round Neck","V-Neck","Collar","Polo","Hooded","Turtle Neck"]"""),
            ("fabric",   "Fabric",   "Text",    false, null),
            ("wash-care","Wash Care", "Text",   false, null),
            ("weight",   "Weight",   "Text",    false, null),
            ("caratage", "Caratage", "Select",  true,  """["14K","18K","22K","24K","925 Silver","Platinum"]"""),
            ("gem-type", "Gem Type", "Select",  true,  """["Diamond","Ruby","Emerald","Sapphire","Pearl","No Gem"]"""),
            ("spf",      "SPF",      "Number",  false, null),
        };

        foreach (var (name, displayName, dataType, isFilterable, allowedValues) in attrs)
        {
            db.AttributeDefinitions.Add(new AttributeDefinition
            {
                Id           = Guid.NewGuid(),
                Name         = name,
                DisplayName  = displayName,
                DataType     = dataType,
                IsFilterable = isFilterable,
                AllowedValues = allowedValues,
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(
        AppDbContext db, Dictionary<string, Guid> cats, Dictionary<string, Guid> brands)
    {
        var products = new List<Product>();
        int imgSeed = 10;
        var rand = new Random(42);

        var catMen       = cats["men"];
        var catWomen     = cats["women"];
        var catKids      = cats["kids"];
        var catFootwear  = cats["footwear"];
        var catJewellery = cats["jewellery"];
        var catBeauty    = cats["beauty"];

        // Seller IDs (first 15 active sellers)
        var sellerIds = await db.Sellers.IgnoreQueryFilters()
            .Where(s => s.Status == SellerStatus.Active)
            .Select(s => s.Id)
            .Take(15)
            .ToListAsync();

        Guid? GetSellerId(int idx) =>
            sellerIds.Count > 0 ? sellerIds[idx % sellerIds.Count] : null;

        string[] menAdj  = { "Classic", "Premium", "Casual", "Formal", "Slim Fit", "Relaxed Fit", "Vintage", "Modern", "Essential", "Signature" };
        string[] menNoun = { "T-Shirt", "Chinos", "Jeans", "Shirt", "Shorts", "Jacket", "Hoodie", "Sweater", "Polo", "Blazer" };
        Guid[] menBrands = [brands["nike"], brands["levis"], brands["hm"], brands["puma"], brands["zara"], brands["adidas"]];
        products.AddRange(GenerateProducts(catMen, menAdj, menNoun, menBrands, "S,M,L,XL", "Black,White,Navy,Grey,Blue", "menswear", ref imgSeed, rand, GetSellerId));

        string[] womenAdj  = { "Elegant", "Casual", "Floral", "Chic", "Vintage", "Modern", "Designer", "Essential", "Classic", "Boho" };
        string[] womenNoun = { "Dress", "Blouse", "Skirt", "Jeans", "Top", "Cardigan", "Jacket", "Jumpsuit", "Leggings", "Tunic" };
        Guid[] womenBrands = [brands["zara"], brands["levis"], brands["hm"], brands["nike"], brands["westside"], brands["fabindia"]];
        products.AddRange(GenerateProducts(catWomen, womenAdj, womenNoun, womenBrands, "XS,S,M,L,XL", "Black,White,Red,Pink,Blue", "womenswear", ref imgSeed, rand, GetSellerId));

        string[] kidsAdj  = { "Cute", "Playful", "Comfy", "Bright", "Cool", "Basic", "Active", "Fun", "Cozy", "Stylish" };
        string[] kidsNoun = { "T-Shirt", "Shorts", "Dress", "Jacket", "Sweater", "Pajamas", "Romper", "Jeans", "Hoodie", "Top" };
        Guid[] kidsBrands = [brands["hm"], brands["levis"], brands["nike"], brands["puma"], brands["adidas"], brands["zara"]];
        products.AddRange(GenerateProducts(catKids, kidsAdj, kidsNoun, kidsBrands, "2-3Y,4-5Y,6-7Y,8-9Y", "Red,Blue,Green,Yellow,Pink", "kidswear", ref imgSeed, rand, GetSellerId));

        string[] footAdj  = { "Comfortable", "Athletic", "Classic", "Stylish", "Premium", "Casual", "Formal", "Running", "Lightweight", "Durable" };
        string[] footNoun = { "Sneakers", "Boots", "Loafers", "Sandals", "Heels", "Flats", "Oxfords", "Slippers", "Trainers", "Wedges" };
        Guid[] footBrands = [brands["nike"], brands["puma"], brands["adidas"], brands["bata"], brands["skechers"], brands["woodland"]];
        products.AddRange(GenerateProducts(catFootwear, footAdj, footNoun, footBrands, "6,7,8,9,10,11", "Black,Brown,White,Navy,Grey", "shoes", ref imgSeed, rand, GetSellerId));

        string[] jewAdj  = { "Elegant", "Stunning", "Classic", "Diamond", "Gold", "Vintage", "Contemporary", "Bridal", "Minimal", "Statement" };
        string[] jewNoun = { "Necklace", "Ring", "Earrings", "Bracelet", "Bangle", "Pendant", "Anklet", "Mangalsutra", "Nose Pin", "Brooch" };
        Guid[] jewBrands = [brands["tanishq"], brands["caratlane"], brands["zara"]];
        products.AddRange(GenerateProducts(catJewellery, jewAdj, jewNoun, jewBrands, "ONE SIZE", "Gold,Rose Gold,Silver,White Gold", "jewellery", ref imgSeed, rand, GetSellerId));

        string[] btyAdj  = { "Hydrating", "Matte", "Radiant", "Anti-Aging", "Natural", "Organic", "Luminous", "Soothing", "Long-Lasting", "Flawless" };
        string[] btyNoun = { "Lipstick", "Foundation", "Serum", "Moisturizer", "Mascara", "Cleanser", "Toner", "Eyeshadow", "Blush", "Primer" };
        Guid[] btyBrands = [brands["lakme"], brands["zara"], brands["hm"]];
        products.AddRange(GenerateProducts(catBeauty, btyAdj, btyNoun, btyBrands, "ONE SIZE", "Regular", "cosmetics", ref imgSeed, rand, GetSellerId));

        const int batchSize = 100;
        for (int i = 0; i < products.Count; i += batchSize)
        {
            var batch = products.Skip(i).Take(batchSize).ToList();
            db.Products.AddRange(batch);
            await db.SaveChangesAsync();
        }
    }

    // ── Admin Content (Banners + Coupons) ─────────────────────────────────────

    private static async Task SeedAdminContentAsync(AppDbContext db)
    {
        if (!await db.Banners.IgnoreQueryFilters().AnyAsync())
        {
            db.Banners.AddRange(
                new Banner { Id = Guid.NewGuid(), Title = "Season Sale — Up to 70% Off", ImageUrl = "https://picsum.photos/seed/banner1/1440/500", LinkUrl = "/products", IsActive = true, DisplayOrder = 1, Placement = BannerPlacement.HeroCarousel },
                new Banner { Id = Guid.NewGuid(), Title = "New Arrivals — Spring 2026", ImageUrl = "https://picsum.photos/seed/banner2/1440/500", LinkUrl = "/products?sort=newest", IsActive = true, DisplayOrder = 2, Placement = BannerPlacement.HeroCarousel },
                new Banner { Id = Guid.NewGuid(), Title = "Premium Jewellery Collection", ImageUrl = "https://picsum.photos/seed/banner3/1440/500", LinkUrl = "/products?category=jewellery", IsActive = true, DisplayOrder = 3, Placement = BannerPlacement.PromoBanner },
                new Banner { Id = Guid.NewGuid(), Title = "Women's Fashion Week Picks", ImageUrl = "https://picsum.photos/seed/banner4/1440/500", LinkUrl = "/products?category=women", IsActive = true, DisplayOrder = 4, Placement = BannerPlacement.PromoBanner },
                new Banner { Id = Guid.NewGuid(), Title = "Kids Summer Collection", ImageUrl = "https://picsum.photos/seed/banner5/1440/500", LinkUrl = "/products?category=kids", IsActive = false, DisplayOrder = 5, Placement = BannerPlacement.CategoryStrip },
                new Banner { Id = Guid.NewGuid(), Title = "Flash Sale — Today Only", ImageUrl = "https://picsum.photos/seed/banner6/1440/500", LinkUrl = "/products?sale=true", IsActive = true, DisplayOrder = 6, Placement = BannerPlacement.FlashSale }
            );
        }

        if (!await db.Coupons.IgnoreQueryFilters().AnyAsync())
        {
            db.Coupons.AddRange(
                new Coupon { Id = Guid.NewGuid(), Code = "WELCOME10", DiscountType = DiscountType.Percentage, DiscountValue = 10, MinOrderAmount = 999, MaxDiscountCap = 500, TotalUsageLimit = 1000, UsedCount = 0, IsActive = true, ExpiresAt = DateTime.UtcNow.AddMonths(6), Description = "10% off on your first order" },
                new Coupon { Id = Guid.NewGuid(), Code = "FLAT200", DiscountType = DiscountType.FlatAmount, DiscountValue = 200, MinOrderAmount = 1499, MaxDiscountCap = 200, TotalUsageLimit = 500, UsedCount = 0, IsActive = true, ExpiresAt = DateTime.UtcNow.AddMonths(3), Description = "Flat ₹200 off on orders above ₹1499" },
                new Coupon { Id = Guid.NewGuid(), Code = "FASHION20", DiscountType = DiscountType.Percentage, DiscountValue = 20, MinOrderAmount = 2000, MaxDiscountCap = 1000, TotalUsageLimit = 200, UsedCount = 0, IsActive = true, ExpiresAt = DateTime.UtcNow.AddMonths(2), Description = "20% off on fashion products" },
                new Coupon { Id = Guid.NewGuid(), Code = "LUXURY15", DiscountType = DiscountType.Percentage, DiscountValue = 15, MinOrderAmount = 5000, MaxDiscountCap = 2000, TotalUsageLimit = 100, UsedCount = 0, IsActive = true, ExpiresAt = DateTime.UtcNow.AddMonths(1), Description = "15% off on luxury brands" },
                new Coupon { Id = Guid.NewGuid(), Code = "FLAT500", DiscountType = DiscountType.FlatAmount, DiscountValue = 500, MinOrderAmount = 2999, MaxDiscountCap = 500, TotalUsageLimit = 300, UsedCount = 0, IsActive = true, ExpiresAt = DateTime.UtcNow.AddMonths(4), Description = "Flat ₹500 off on orders above ₹2999" }
            );
        }

        await db.SaveChangesAsync();
    }

    // ── Product generation helper ─────────────────────────────────────────────

    private static List<Product> GenerateProducts(
        Guid categoryId, string[] adjectives, string[] nouns, Guid[] brands,
        string sizes, string colors, string imageKeyword, ref int imgSeed, Random rand,
        Func<int, Guid?> getSellerId)
    {
        var list = new List<Product>();
        int productIdx = 0;

        foreach (var adj in adjectives)
        {
            foreach (var noun in nouns)
            {
                var productId = Guid.NewGuid();
                var name = $"{adj} {noun}";
                var basePrice = Math.Round((decimal)(rand.NextDouble() * 5000 + 500) / 100) * 100 - 1;
                var discPrice = rand.NextDouble() > 0.3
                    ? basePrice - (Math.Round((decimal)(rand.NextDouble() * 1000 + 100) / 100) * 100)
                    : (decimal?)null;
                if (discPrice <= 0) discPrice = basePrice * 0.8m;

                var brandId  = brands[rand.Next(brands.Length)];
                var slug     = Slugify($"{name}-{imgSeed}");
                var sellerId = getSellerId(productIdx++);

                var variants = new List<ProductVariant>();
                int skuIdx   = 0;
                var sizeArr  = sizes.Split(',');
                var colorArr = colors.Split(',');
                var selSizes  = sizeArr.OrderBy(_ => rand.Next()).Take(2).ToArray();
                var selColors = colorArr.OrderBy(_ => rand.Next()).Take(2).ToArray();

                foreach (var size in selSizes)
                foreach (var colour in selColors)
                {
                    variants.Add(new ProductVariant
                    {
                        Id            = Guid.NewGuid(),
                        ProductId     = productId,
                        Size          = size.Trim(),
                        Colour        = colour.Trim(),
                        Sku           = $"{slug[..Math.Min(slug.Length, 20)]}-{skuIdx++}",
                        StockQuantity = rand.Next(5, 150),
                        PriceOverride = null,
                    });
                }

                var images = new List<ProductImage>
                {
                    new() { Id = Guid.NewGuid(), ProductId = productId, Url = $"https://picsum.photos/seed/{imageKeyword}{imgSeed}/600/800", DisplayOrder = 0, IsPrimary = true },
                    new() { Id = Guid.NewGuid(), ProductId = productId, Url = $"https://picsum.photos/seed/{imageKeyword}{imgSeed + 1}/600/800", DisplayOrder = 1, IsPrimary = false },
                };
                imgSeed += 2;

                list.Add(new Product
                {
                    Id              = productId,
                    Name            = name,
                    Slug            = slug,
                    Description     = $"A premium quality {name.ToLowerInvariant()} crafted for style and comfort.",
                    BasePrice       = basePrice,
                    DiscountedPrice = discPrice,
                    CategoryId      = categoryId,
                    BrandId         = brandId,
                    SellerId        = sellerId,
                    AverageRating   = Math.Round(3.5 + rand.NextDouble() * 1.5, 1),
                    ReviewCount     = rand.Next(10, 500),
                    IsActive        = true,
                    Variants        = variants,
                    Images          = images,
                });
            }
        }

        return list;
    }

    private static string Slugify(string name) =>
        name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("&", "and")
            .Replace("/", "-")
            .Replace("(", "")
            .Replace(")", "");

    // ── Seller store names ────────────────────────────────────────────────────

    private static readonly string[] SellerStoreNames =
    [
        "Fashion Hub", "Style Street", "Trendy Threads", "Chic Boutique", "Urban Wear",
        "Ethnic Elegance", "Luxe Collections", "The Fashion Lab", "Couture Corner", "Wardrobe Essentials",
        "Silk & Satin", "Denim Dreams", "Premier Fashion", "The Style Loft", "Fashion Forward",
        "Glam Studio", "Vogue Vault", "Style Sphere", "Trend Setter", "My Wardrobe",
    ];
}
