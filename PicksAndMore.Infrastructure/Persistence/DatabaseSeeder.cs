using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Domain.Enums;
using PicksAndMore.Domain.ValueObjects;

namespace PicksAndMore.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 1. Seed Roles
        var adminRole = new Role(Guid.Parse("a5e2f7b4-3c82-411a-85d1-12c8a2bbdd01"), "Admin", "Enterprise Super Administrator");
        var managerRole = new Role(Guid.Parse("b2c3d4e5-f6a7-4811-9a3b-23c4d5e6f7a8"), "ProductManager", "Product and Category Manager");
        var logisticsRole = new Role(Guid.Parse("c3d4e5f6-a7b8-4911-8c4d-34d5e6f7a8b9"), "SupportLogistics", "Logistics and Support Operator");

        if (!await context.Roles.AnyAsync())
        {
            await context.Roles.AddRangeAsync(adminRole, managerRole, logisticsRole);
            await context.SaveChangesAsync();
        }

        // 2. Seed Role Permissions
        var hasGranularPermissions = await context.RolePermissions.AnyAsync(rp => rp.Permission == "Products:Read");
        if (!hasGranularPermissions)
        {
            // Clear old non-granular permissions
            context.RolePermissions.RemoveRange(context.RolePermissions);
            await context.SaveChangesAsync();

            var permissions = new List<RolePermission>();

            var features = new[] { "Products", "Orders", "Shipping", "Analytics", "PromoCodes", "Roles" };
            var actions = new[] { "Read", "Create", "Update", "Delete" };

            // Admin: Assign all permissions (all 24 CRUD combinations across 6 features)
            foreach (var feature in features)
            {
                foreach (var action in actions)
                {
                    permissions.Add(new RolePermission(Guid.NewGuid(), adminRole.Id, $"{feature}:{action}"));
                }
            }

            // ProductManager: Assign Products and PromoCodes CRUD permissions
            foreach (var action in actions)
            {
                permissions.Add(new RolePermission(Guid.NewGuid(), managerRole.Id, $"Products:{action}"));
                permissions.Add(new RolePermission(Guid.NewGuid(), managerRole.Id, $"PromoCodes:{action}"));
            }

            // SupportLogistics: Assign Orders and Shipping Read/Update
            permissions.Add(new RolePermission(Guid.NewGuid(), logisticsRole.Id, "Orders:Read"));
            permissions.Add(new RolePermission(Guid.NewGuid(), logisticsRole.Id, "Orders:Update"));
            permissions.Add(new RolePermission(Guid.NewGuid(), logisticsRole.Id, "Shipping:Read"));
            permissions.Add(new RolePermission(Guid.NewGuid(), logisticsRole.Id, "Shipping:Update"));

            await context.RolePermissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();
        }

        // 3. Seed Default Admin User
        if (!await context.Users.AnyAsync(u => u.Email == "admin@picksandmore.com"))
        {
            var adminUser = new ApplicationUser
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-47a8-b9c0-12d3e4f5a6b7"),
                FullName = "System Administrator",
                Email = "admin@picksandmore.com",
                NormalizedEmail = "ADMIN@PICKSANDMORE.COM",
                UserName = "admin@picksandmore.com",
                NormalizedUserName = "ADMIN@PICKSANDMORE.COM",
                EmailConfirmed = true,
                IsGuest = false,
                RoleId = adminRole.Id,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var hasher = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@123");

            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }

        // Seed default notification subscription for the admin user to receive "NewOrder" alerts
        var adminUserId = Guid.Parse("a1b2c3d4-e5f6-47a8-b9c0-12d3e4f5a6b7");
        var hasSubscription = await context.NotificationSubscriptions.AnyAsync(ns => ns.UserId == adminUserId && ns.NotificationType == "NewOrder");
        if (!hasSubscription)
        {
            await context.NotificationSubscriptions.AddAsync(new NotificationSubscription(
                Guid.NewGuid(),
                adminUserId,
                "NewOrder"
            )
            {
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            });
            await context.SaveChangesAsync();
            Console.WriteLine("Successfully seeded NewOrder notification subscription for default Admin user.");
        }

        // Clear existing product-related data to seed clean with new many-to-many categories and age fields
        context.ProductReviews.RemoveRange(context.ProductReviews);
        context.OrderItems.RemoveRange(context.OrderItems);
        context.Orders.RemoveRange(context.Orders);
        context.ProductImages.RemoveRange(context.ProductImages);
        context.ProductMetadata.RemoveRange(context.ProductMetadata);
        context.Products.RemoveRange(context.Products);
        context.Categories.RemoveRange(context.Categories);
        await context.SaveChangesAsync();

        // 4. Seed Categories (Subcategories)
        var categories = new List<Category>
        {
            // Women subcategories
            new Category(Guid.Parse("11111111-1111-1111-1111-111111111111"), "fashion", "Women", true),
            new Category(Guid.Parse("22222222-2222-2222-2222-222222222222"), "pajama", "Women", true),
            new Category(Guid.Parse("33333333-3333-3333-3333-333333333333"), "bags", "Women", true),
            new Category(Guid.Parse("44444444-4444-4444-4444-444444444444"), "shoes", "Women", true),
            new Category(Guid.Parse("55555555-5555-5555-5555-555555555555"), "accessors", "Women", true),
            // Kids subcategories
            new Category(Guid.Parse("66666666-6666-6666-6666-666666666666"), "kids boys", "Kids", true),
            new Category(Guid.Parse("77777777-7777-7777-7777-777777777777"), "girls", "Kids", true),
            new Category(Guid.Parse("88888888-8888-8888-8888-888888888888"), "unisex collection", "Kids", true),
            new Category(Guid.Parse("99999999-9999-9999-9999-999999999999"), "baby needs", "Kids", true),
            new Category(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "accessors", "Kids", true)
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        // Seed Brands
        var brandZaraId = Guid.Parse("f6a8e8b9-1111-2222-3333-444455556666");
        var brandGucciId = Guid.Parse("f6a8e8b9-2222-3333-4444-555566667777");
        var brandChanelId = Guid.Parse("f6a8e8b9-3333-4444-5555-666677778888");
        var brandNikeId = Guid.Parse("f6a8e8b9-4444-5555-6666-777788889999");
        var brandPradaId = Guid.Parse("f6a8e8b9-5555-6666-7777-88889999aaaa");

        var brands = new List<Brand>
        {
            new Brand(brandZaraId, "Zara", "/brands/zara.png", showInCards: true, isVisible: true),
            new Brand(brandGucciId, "Gucci", "/brands/gucci.png", showInCards: true, isVisible: true),
            new Brand(brandChanelId, "Chanel", "/brands/chanel.png", showInCards: true, isVisible: true),
            new Brand(brandNikeId, "Nike", "/brands/nike.png", showInCards: false, isVisible: true),
            new Brand(brandPradaId, "Prada", "/brands/prada.png", showInCards: true, isVisible: true)
        };

        if (!await context.Brands.AnyAsync())
        {
            await context.Brands.AddRangeAsync(brands);
            await context.SaveChangesAsync();
        }

        // 5. Seed Products with Age and Multiple Categories
        var seedProducts = new List<Product>();

        // Product 1: Sculpted Leather Handbag
        var product1 = new Product(
            Guid.Parse("d3b07384-d113-40e1-a3f2-861f2113d077"),
            "Sculpted Leather Handbag",
            "Exquisite silhouettes handcrafted from Italian full-grain leather. Designed for organic structures.",
            850.00m,
            510.00m,
            15,
            true,
            null,
            categories[2].Id, // bags (Women)
            ShippingSize.Medium,
            "/products/handbag.png"
        ) { Age = null, BrandId = brandGucciId, CollectionType = "Bestsellers" };
        product1.Categories.Add(categories[2]); // bags
        product1.Categories.Add(categories[4]); // accessors
        seedProducts.Add(product1);

        // Product 2: Stiletto Heels
        var product2 = new Product(
            Guid.Parse("3e5c706d-e4c1-40e1-85f2-95fcd773d100"),
            "Stiletto Heels",
            "Precision engineering meets high fashion. Designed to catch light with beautiful curvature and elegant straps.",
            620.00m,
            372.00m,
            24,
            true,
            null,
            categories[3].Id, // shoes (Women)
            ShippingSize.Small,
            "/products/heels.png"
        ) { Age = null, BrandId = brandPradaId };
        product2.Categories.Add(categories[3]); // shoes
        product2.Categories.Add(categories[0]); // fashion
        seedProducts.Add(product2);

        // Product 3: Flowing Satin Slip
        var product3 = new Product(
            Guid.Parse("c12d7b56-ea12-40e1-b85f-861f2113d099"),
            "Flowing Satin Slip",
            "Sleek flowing satin slip dress made of 100% pure mulberry silk.",
            340.00m,
            204.00m,
            30,
            true,
            null,
            categories[0].Id, // fashion (Women)
            ShippingSize.Small,
            "/products/dress.png"
        ) { Age = null, BrandId = brandChanelId, CollectionType = "Latest" };
        product3.Categories.Add(categories[0]); // fashion
        product3.Categories.Add(categories[1]); // pajama
        seedProducts.Add(product3);

        // Product 4: Asymmetric Knit Dress
        var product4 = new Product(
            Guid.Parse("e8c2057d-a113-40e1-85f2-a392039f3001"),
            "Asymmetric Knit Dress",
            "Elegant asymmetric ribbed knit dress tailored to drape beautifully.",
            480.00m,
            288.00m,
            12,
            true,
            null,
            categories[0].Id, // fashion (Women)
            ShippingSize.Small,
            "/products/dress.png"
        ) { Age = null, BrandId = brandZaraId };
        product4.Categories.Add(categories[0]); // fashion
        seedProducts.Add(product4);

        // Product 5: Silk Organza Gown
        var product5 = new Product(
            Guid.Parse("182c0bda-87e2-40e1-859a-1122a2bbcd34"),
            "Silk Organza Gown",
            "Show-stopping silk organza gown featuring structured corsetry and layered hem.",
            1200.00m,
            720.00m,
            5,
            true,
            null,
            categories[0].Id, // fashion (Women)
            ShippingSize.Medium,
            "/products/dress.png"
        ) { Age = null, BrandId = brandChanelId, CollectionType = "Featured" };
        product5.Categories.Add(categories[0]); // fashion
        seedProducts.Add(product5);

        // Product 6: The Luxury Diaper Bag
        var product6 = new Product(
            Guid.Parse("d3b07384-d113-40e1-a3f2-861f2113d088"),
            "The Luxury Diaper Bag",
            "A highly functional diaper bag crafted with waterproof linen lining, gold-tone hardware, and premium leather straps.",
            390.00m,
            234.00m,
            40,
            true,
            null,
            categories[8].Id, // baby needs (Kids)
            ShippingSize.Medium,
            "/products/diaper_bag.png"
        ) { Age = "0-12 Months", BrandId = brandGucciId };
        product6.Categories.Add(categories[8]); // baby needs
        product6.Categories.Add(categories[9]); // accessors (Kids)
        seedProducts.Add(product6);

        // Product 7: Junior Active Sneakers
        var product7 = new Product(
            Guid.Parse("3e7748cd-24fa-4011-85f2-95fcd773d100"),
            "Junior Active Sneakers",
            "Soft knit uppers and lightweight, high-traction soles designed to protect growing feet.",
            110.00m,
            66.00m,
            50,
            true,
            null,
            categories[5].Id, // kids boys (Kids)
            ShippingSize.Small,
            "/products/sneaker.png"
        ) { Age = "4-6 Years", BrandId = brandNikeId, CollectionType = "On Sale" };
        product7.Categories.Add(categories[5]); // kids boys
        product7.Categories.Add(categories[7]); // unisex collection
        seedProducts.Add(product7);

        // Product 8: Baby Organic Cotton Romper
        var product8 = new Product(
            Guid.Parse("4e8859de-35fb-4122-9603-a60de884e200"),
            "Baby Organic Cotton Romper",
            "Ultra-soft organic cotton romper designed for baby's daily comfort with easy snap buttons.",
            180.00m,
            108.00m,
            35,
            true,
            null,
            categories[7].Id, // unisex collection (Kids)
            ShippingSize.Small,
            "/products/infant_dress.png"
        ) { Age = "0-12 Months", BrandId = brandZaraId, CollectionType = "Featured" };
        product8.Categories.Add(categories[7]); // unisex collection
        product8.Categories.Add(categories[8]); // baby needs
        seedProducts.Add(product8);

        await context.Products.AddRangeAsync(seedProducts);
        await context.SaveChangesAsync();

        // 6. Seed ProductMetadata
        var metadata = new List<ProductMetadata>
        {
            new ProductMetadata(Guid.NewGuid(), Guid.Parse("d3b07384-d113-40e1-a3f2-861f2113d077"), new List<string> { "One Size" }, new List<string> { "Tan", "Black" }, new List<string> { "Leather" }, new List<string> { "All-Season" }),
            new ProductMetadata(Guid.NewGuid(), Guid.Parse("3e5c706d-e4c1-40e1-85f2-95fcd773d100"), new List<string> { "37", "38", "39" }, new List<string> { "Gold", "Silver" }, new List<string> { "Synthetic" }, new List<string> { "Summer" }),
            new ProductMetadata(Guid.NewGuid(), Guid.Parse("c12d7b56-ea12-40e1-b85f-861f2113d099"), new List<string> { "S", "M", "L" }, new List<string> { "Champagne", "Emerald" }, new List<string> { "Silk" }, new List<string> { "Summer" }),
            new ProductMetadata(Guid.NewGuid(), Guid.Parse("e8c2057d-a113-40e1-85f2-a392039f3001"), new List<string> { "XS", "S", "M", "L" }, new List<string> { "Oatmeal", "Charcoal" }, new List<string> { "Wool Blend" }, new List<string> { "Winter" }),
            new ProductMetadata(Guid.NewGuid(), Guid.Parse("182c0bda-87e2-40e1-859a-1122a2bbcd34"), new List<string> { "S", "M" }, new List<string> { "Blush", "Ivory" }, new List<string> { "Organza" }, new List<string> { "Spring" }),
            new ProductMetadata(Guid.NewGuid(), Guid.Parse("d3b07384-d113-40e1-a3f2-861f2113d088"), new List<string> { "One Size" }, new List<string> { "Taupe", "Sage" }, new List<string> { "Linen", "Leather" }, new List<string> { "All-Season" }),
            new ProductMetadata(Guid.NewGuid(), Guid.Parse("3e7748cd-24fa-4011-85f2-95fcd773d100"), new List<string> { "EU 28", "EU 29", "EU 30.5", "EU 32" }, new List<string> { "Blue/Orange", "White/Green" }, new List<string> { "Mesh" }, new List<string> { "All-Season" }),
            new ProductMetadata(Guid.NewGuid(), Guid.Parse("4e8859de-35fb-4122-9603-a60de884e200"), new List<string> { "3-6 Months (62-68cm)", "6-9 Months (68-74cm)", "9-12 Months (74-80cm)" }, new List<string> { "Ivory", "Sage", "Blush" }, new List<string> { "Organic Cotton" }, new List<string> { "All-Season" })
        };

        await context.ProductMetadata.AddRangeAsync(metadata);
        await context.SaveChangesAsync();

        // 7. Seed ProductImages (multiple images per product)
        var productImages = new List<ProductImage>
        {
            // Sculpted Leather Handbag
            new ProductImage(Guid.NewGuid(), Guid.Parse("d3b07384-d113-40e1-a3f2-861f2113d077"), "/products/handbag.png", 0, "Handbag front view"),
            new ProductImage(Guid.Parse("11223344-5566-7788-9900-aabbccddeeff"), Guid.Parse("d3b07384-d113-40e1-a3f2-861f2113d077"), "/products/handbag_2.png", 1, "Handbag angle view"),
            // Stiletto Heels
            new ProductImage(Guid.NewGuid(), Guid.Parse("3e5c706d-e4c1-40e1-85f2-95fcd773d100"), "/products/heels.png", 0, "Heels front view"),
            // Flowing Satin Slip
            new ProductImage(Guid.NewGuid(), Guid.Parse("c12d7b56-ea12-40e1-b85f-861f2113d099"), "/products/dress.png", 0, "Satin slip full view"),
            new ProductImage(Guid.Parse("22334455-6677-8899-00aa-bbccddeeff11"), Guid.Parse("c12d7b56-ea12-40e1-b85f-861f2113d099"), "/products/casual_dress_2.png", 1, "Satin slip detail"),
            // Asymmetric Knit Dress
            new ProductImage(Guid.NewGuid(), Guid.Parse("e8c2057d-a113-40e1-85f2-a392039f3001"), "/products/dress.png", 0, "Knit dress front"),
            // Silk Organza Gown
            new ProductImage(Guid.NewGuid(), Guid.Parse("182c0bda-87e2-40e1-859a-1122a2bbcd34"), "/products/dress.png", 0, "Gown full view"),
            // The Luxury Diaper Bag
            new ProductImage(Guid.NewGuid(), Guid.Parse("d3b07384-d113-40e1-a3f2-861f2113d088"), "/products/diaper_bag.png", 0, "Diaper bag front"),
            new ProductImage(Guid.Parse("33445566-7788-9900-00aa-bbccddeeff22"), Guid.Parse("d3b07384-d113-40e1-a3f2-861f2113d088"), "/products/baby_bib.png", 1, "Diaper bag content"),
            // Junior Active Sneakers
            new ProductImage(Guid.NewGuid(), Guid.Parse("3e7748cd-24fa-4011-85f2-95fcd773d100"), "/products/sneaker.png", 0, "Sneakers front view"),
            new ProductImage(Guid.Parse("44556677-8899-00aa-9900-bbccddeeff33"), Guid.Parse("3e7748cd-24fa-4011-85f2-95fcd773d100"), "/products/baby_clogs.png", 1, "Sneakers alternate"),
            // Baby Organic Cotton Romper
            new ProductImage(Guid.NewGuid(), Guid.Parse("4e8859de-35fb-4122-9603-a60de884e200"), "/products/infant_dress.png", 0, "Romper front view"),
            new ProductImage(Guid.Parse("55667788-9900-00aa-9900-bbccddeeff44"), Guid.Parse("4e8859de-35fb-4122-9603-a60de884e200"), "/products/baby_bib.png", 1, "Romper flat lay")
        };

        await context.ProductImages.AddRangeAsync(productImages);
        await context.SaveChangesAsync();

        // 8. Seed ProductReviews
        var adminUserForReviews = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@picksandmore.com");
        if (adminUserForReviews != null)
        {
            var silkGownId = Guid.Parse("182c0bda-87e2-40e1-859a-1122a2bbcd34");

            var seedReviews = new List<ProductReview>
            {
                new ProductReview(Guid.NewGuid(), silkGownId, adminUserForReviews.Id, "Sophia Loren", "An absolutely exquisite specimen. The tailoring and organic fabric feel incredibly premium.", 5),
                new ProductReview(Guid.NewGuid(), silkGownId, adminUserForReviews.Id, "Marcus Aurelius", "Remarkable material composition. Highly durable yet maintains a soft, lightweight texture.", 4),
                new ProductReview(Guid.NewGuid(), silkGownId, adminUserForReviews.Id, "Isabella Rossellini", "Delicate, refined, and fits perfectly. A true masterpiece of modern style.", 5)
            };

            foreach (var r in seedReviews)
            {
                r.CreatedAt = DateTime.UtcNow;
                r.CreatedBy = "System";
            }

            await context.ProductReviews.AddRangeAsync(seedReviews);
            await context.SaveChangesAsync();
        }

        // 9. Seed Shipping Matrices (all 27 Egypt Governorates)
        if (!await context.ShippingMatrices.AnyAsync())
        {
            var matrices = new List<ShippingMatrix>
            {
                // Cairo/Giza
                new ShippingMatrix(Guid.NewGuid(), "Cairo", 35.00m, 55.00m, 85.00m),
                new ShippingMatrix(Guid.NewGuid(), "Giza", 35.00m, 55.00m, 85.00m),
                // Alexandria/Canal
                new ShippingMatrix(Guid.NewGuid(), "Alexandria", 40.00m, 60.00m, 90.00m),
                new ShippingMatrix(Guid.NewGuid(), "Port Said", 40.00m, 60.00m, 90.00m),
                new ShippingMatrix(Guid.NewGuid(), "Suez", 40.00m, 60.00m, 90.00m),
                new ShippingMatrix(Guid.NewGuid(), "Ismailia", 40.00m, 60.00m, 90.00m),
                // Delta
                new ShippingMatrix(Guid.NewGuid(), "Qalyubia", 45.00m, 65.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Dakahlia", 45.00m, 65.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Sharqia", 45.00m, 65.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Kafr El Sheikh", 45.00m, 65.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Gharbia", 45.00m, 65.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Monufia", 45.00m, 65.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Beheira", 45.00m, 65.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Damietta", 45.00m, 65.00m, 95.00m),
                // Upper Egypt
                new ShippingMatrix(Guid.NewGuid(), "Fayoum", 50.00m, 70.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Beni Suef", 50.00m, 70.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Minya", 50.00m, 70.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Assiut", 50.00m, 70.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Sohag", 50.00m, 70.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Qena", 50.00m, 70.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Luxor", 50.00m, 70.00m, 95.00m),
                new ShippingMatrix(Guid.NewGuid(), "Aswan", 50.00m, 70.00m, 95.00m),
                // Frontier
                new ShippingMatrix(Guid.NewGuid(), "Red Sea", 60.00m, 85.00m, 110.00m),
                new ShippingMatrix(Guid.NewGuid(), "New Valley", 60.00m, 85.00m, 110.00m),
                new ShippingMatrix(Guid.NewGuid(), "Matrouh", 60.00m, 85.00m, 110.00m),
                new ShippingMatrix(Guid.NewGuid(), "North Sinai", 60.00m, 85.00m, 110.00m),
                new ShippingMatrix(Guid.NewGuid(), "South Sinai", 60.00m, 85.00m, 110.00m)
            };
            await context.ShippingMatrices.AddRangeAsync(matrices);
            await context.SaveChangesAsync();
        }

        // 10. Seed Shipping Combo Rules
        if (!await context.ShippingComboRules.AnyAsync())
        {
            var rules = new List<ShippingComboRule>
            {
                new ShippingComboRule(Guid.NewGuid(), 1, 1, ShippingSize.Large),
                new ShippingComboRule(Guid.NewGuid(), 4, 0, ShippingSize.Large),
                new ShippingComboRule(Guid.NewGuid(), 3, 0, ShippingSize.Medium)
            };
            await context.ShippingComboRules.AddRangeAsync(rules);
            await context.SaveChangesAsync();
        }

        // 11. Seed a Test Order for active logistics tracking demonstration
        if (!await context.Orders.AnyAsync(o => o.Id == Guid.Parse("11111111-2222-3333-4444-555555555555")))
        {
            var adminUserForOrder = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@picksandmore.com");
            if (adminUserForOrder != null)
            {
                var testOrder = new Order(
                    Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    adminUserForOrder.Id,
                    DateTime.UtcNow.AddHours(-2),
                    1735.00m,
                    35.00m,
                    OrderStatus.ConfirmedPreparing,
                    PaymentMethod.COD,
                    new Address(
                        "Cairo",
                        "12 Luxury Boulevard, Fifth Settlement, New Cairo",
                        "01002345678"
                    )
                );
                testOrder.AddOrderItem(Guid.Parse("d3b07384-d113-40e1-a3f2-861f2113d077"), 2, 850.00m); // Sculpted Leather Handbag
                testOrder.CreatedAt = DateTime.UtcNow;
                testOrder.CreatedBy = "System";

                await context.Orders.AddAsync(testOrder);
                await context.SaveChangesAsync();
                Console.WriteLine("Successfully seeded test order 11111111-2222-3333-4444-555555555555 for tracking.");
            }
        }

        // Ensure Analytics:Read permission is seeded for Admin role
        var adminRoleId = Guid.Parse("a5e2f7b4-3c82-411a-85d1-12c8a2bbdd01");
        var hasAnalyticsRead = await context.RolePermissions.AnyAsync(rp => rp.RoleId == adminRoleId && rp.Permission == "Analytics:Read");
        if (!hasAnalyticsRead)
        {
            context.RolePermissions.Add(new RolePermission(Guid.NewGuid(), adminRoleId, "Analytics:Read"));
            await context.SaveChangesAsync();
            Console.WriteLine("Retroactively seeded Analytics:Read permission for Admin role.");
        }
    }
}
