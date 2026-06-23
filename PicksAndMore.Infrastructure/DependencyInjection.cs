using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Services;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;
using PicksAndMore.Infrastructure.Repositories;
using PicksAndMore.Infrastructure.Services;

namespace PicksAndMore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Register DbContext using Npgsql PostgreSQL provider
        // Support Render.com DATABASE_URL environment variable (postgres:// format) or standard connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrEmpty(databaseUrl))
        {
            // Parse Render.com postgres:// URL format into Npgsql connection string
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
            )
        );

        // 2. Register Repositories, Services, and Unit of Work
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IShippingMatrixRepository, ShippingMatrixRepository>();
        services.AddScoped<IShippingComboRuleRepository, ShippingComboRuleRepository>();
        services.AddScoped<IProductShippingOverrideRepository, ProductShippingOverrideRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationSubscriptionRepository, NotificationSubscriptionRepository>();
        services.AddScoped<IColorOptionRepository, ColorOptionRepository>();
        services.AddScoped<ISizeOptionRepository, SizeOptionRepository>();

        // 3. Register HTTP Context Accessor and Current User Service
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 4. Register Security & Token generation services
        services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
