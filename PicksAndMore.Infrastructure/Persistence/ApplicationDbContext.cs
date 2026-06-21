using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Application.Interfaces;

namespace PicksAndMore.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, Role, Guid>
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? "System";
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        var userId = _currentUserService.UserId ?? "System";
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        return base.SaveChanges();
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ProductMetadata> ProductMetadata { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<ShippingMatrix> ShippingMatrices { get; set; }
    public DbSet<ShippingComboRule> ShippingComboRules { get; set; }
    public DbSet<ProductShippingOverride> ProductShippingOverrides { get; set; }
    public DbSet<DigitalWalletVerification> DigitalWalletVerifications { get; set; }
    public DbSet<PromoCode> PromoCodes { get; set; }
    public DbSet<ProductReview> ProductReviews { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<VisitorLog> VisitorLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ASP.NET Identity Entities Custom properties
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            entity.Property(u => u.SecondaryPhoneNumber).HasMaxLength(20);
            entity.Property(u => u.AddressDetails).HasMaxLength(500);
            entity.Property(u => u.Governorate).HasMaxLength(100);
        });

        // Configure Brand Entity
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("Brands");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Name).IsRequired().HasMaxLength(100);
            entity.Property(b => b.LogoUrl).IsRequired().HasMaxLength(500);
            entity.Property(b => b.ShowInCards).IsRequired();
            entity.Property(b => b.IsVisible).IsRequired();
        });

        // Configure Category Entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.TargetAudience).IsRequired().HasMaxLength(100);
            entity.Property(c => c.IsVisible).IsRequired();
            
            entity.HasMany(c => c.Products)
                  .WithOne(p => p.Category)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Product Entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.StockQuantity).IsRequired();
            entity.Property(p => p.IsVisible).IsRequired();
            entity.Property(p => p.ScheduledPublishDate);
            entity.Property(p => p.ShippingSize).HasConversion<string>().HasMaxLength(50);
            entity.Property(p => p.ImageUrl).HasMaxLength(500);
            entity.Property(p => p.CollectionType).HasMaxLength(100);

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Brand)
                  .WithMany(b => b.Products)
                  .HasForeignKey(p => p.BrandId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.Metadata)
                  .WithOne(m => m.Product)
                  .HasForeignKey<ProductMetadata>(m => m.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Images)
                  .WithOne(i => i.Product)
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Categories)
                  .WithMany()
                  .UsingEntity<Dictionary<string, object>>(
                      "ProductCategories",
                      j => j.HasOne<Category>().WithMany().HasForeignKey("CategoryId").OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<Product>().WithMany().HasForeignKey("ProductId").OnDelete(DeleteBehavior.Cascade)
                  );
        });

        // Configure ProductImage Entity
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Url).IsRequired().HasMaxLength(500);
            entity.Property(i => i.SortOrder).IsRequired();
            entity.Property(i => i.AltText).HasMaxLength(300);
        });

        // Configure ProductMetadata Entity
        modelBuilder.Entity<ProductMetadata>(entity =>
        {
            entity.ToTable("ProductMetadata");
            entity.HasKey(m => m.Id);

            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2,
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            entity.Property(m => m.Sizes)
                  .HasConversion(
                      s => string.Join(',', s),
                      s => s.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                  )
                  .Metadata.SetValueComparer(stringListComparer);

            entity.Property(m => m.Colors)
                  .HasConversion(
                      c => string.Join(',', c),
                      c => c.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                  )
                  .Metadata.SetValueComparer(stringListComparer);

            entity.Property(m => m.Materials)
                  .HasConversion(
                      m => string.Join(',', m),
                      m => m.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                  )
                  .Metadata.SetValueComparer(stringListComparer);

            entity.Property(m => m.SeasonTags)
                  .HasConversion(
                      s => string.Join(',', s),
                      s => s.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                  )
                  .Metadata.SetValueComparer(stringListComparer);
        });

        // Configure Order Entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(o => o.ShippingCost).HasColumnType("decimal(18,2)");
            entity.Property(o => o.OrderStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(50);
            entity.Property(o => o.OrderDate).IsRequired();

            entity.OwnsOne(o => o.ShippingAddress, address =>
            {
                address.Property(a => a.Governorate).HasColumnName("Shipping_Governorate").HasMaxLength(100).IsRequired();
                address.Property(a => a.DetailedAddress).HasColumnName("Shipping_DetailedAddress").HasMaxLength(500).IsRequired();
                address.Property(a => a.PrimaryPhone).HasColumnName("Shipping_PrimaryPhone").HasMaxLength(20).IsRequired();
                address.Property(a => a.SecondaryPhone).HasColumnName("Shipping_SecondaryPhone").HasMaxLength(20).IsRequired(false);
            });

            entity.HasMany(o => o.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(o => o.Items)
                  .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasOne(o => o.WalletVerification)
                  .WithOne(v => v.Order)
                  .HasForeignKey<DigitalWalletVerification>(v => v.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.User)
                  .WithMany()
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure OrderItem Entity
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(oi => oi.Quantity).IsRequired();
            entity.Property(oi => oi.IsReturnedPartially).IsRequired();
            entity.Property(oi => oi.OriginalQuantity).IsRequired();

            entity.HasOne(oi => oi.Product)
                  .WithMany()
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure DigitalWalletVerification Entity
        modelBuilder.Entity<DigitalWalletVerification>(entity =>
        {
            entity.ToTable("DigitalWalletVerifications");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.ScreenshotUrl).IsRequired().HasMaxLength(500);
            entity.Property(v => v.SenderPhoneNumberOrName).IsRequired().HasMaxLength(100);
            entity.Property(v => v.IsVerified).IsRequired();
            entity.Property(v => v.RejectionReason).HasMaxLength(500);

            entity.HasOne(v => v.VerifiedByUser)
                  .WithMany()
                  .HasForeignKey(v => v.VerifiedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ShippingMatrix Entity
        modelBuilder.Entity<ShippingMatrix>(entity =>
        {
            entity.ToTable("ShippingMatrices");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Governorate).IsRequired().HasMaxLength(100);
            entity.Property(s => s.BasePriceSmall).HasColumnType("decimal(18,2)");
            entity.Property(s => s.BasePriceMedium).HasColumnType("decimal(18,2)");
            entity.Property(s => s.BasePriceLarge).HasColumnType("decimal(18,2)");
            entity.HasIndex(s => s.Governorate).IsUnique();
        });

        // Configure ShippingComboRule Entity
        modelBuilder.Entity<ShippingComboRule>(entity =>
        {
            entity.ToTable("ShippingComboRules");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.InputSmallCount).IsRequired();
            entity.Property(r => r.InputMediumCount).IsRequired();
            entity.Property(r => r.ResultingSize).HasConversion<string>().HasMaxLength(50);
        });

        // Configure ProductShippingOverride Entity
        modelBuilder.Entity<ProductShippingOverride>(entity =>
        {
            entity.ToTable("ProductShippingOverrides");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.IsFreeShipping).IsRequired();
            entity.Property(o => o.FixedPrice).HasColumnType("decimal(18,2)");

            entity.HasOne(o => o.Product)
                  .WithMany()
                  .HasForeignKey(o => o.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RolePermission Entity
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(rp => rp.Id);
            entity.Property(rp => rp.RoleId).IsRequired();
            entity.Property(rp => rp.Permission).IsRequired().HasMaxLength(100);
            entity.HasIndex(rp => new { rp.RoleId, rp.Permission }).IsUnique();
        });

        // Configure PromoCode Entity
        modelBuilder.Entity<PromoCode>(entity =>
        {
            entity.ToTable("PromoCodes");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Value).HasColumnType("decimal(18,2)");
            entity.Property(p => p.MinOrderAmount).HasColumnType("decimal(18,2)");
            entity.Property(p => p.DiscountType).HasConversion<string>().HasMaxLength(50);
        });

        // Configure ProductReview Entity
        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.ToTable("ProductReviews");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.ReviewerName).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Comment).IsRequired().HasMaxLength(2000);
            entity.Property(r => r.Rating).IsRequired();

            entity.HasOne(r => r.Product)
                  .WithMany()
                  .HasForeignKey(r => r.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                  .WithMany()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure VisitorLog Entity
        modelBuilder.Entity<VisitorLog>(entity =>
        {
            entity.ToTable("VisitorLogs");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.IpAddress).IsRequired().HasMaxLength(50);
            entity.Property(v => v.Country).IsRequired().HasMaxLength(100);
            entity.Property(v => v.Governorate).IsRequired().HasMaxLength(100);
            entity.Property(v => v.Timestamp).IsRequired();
        });
    }
}
