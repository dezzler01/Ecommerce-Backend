using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using PicksAndMore.API.Authorization;
using PicksAndMore.Infrastructure;
using PicksAndMore.Infrastructure.Persistence;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Application.Hubs;
using Microsoft.EntityFrameworkCore;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add controller endpoints support
builder.Services.AddControllers();

// Register SignalR services
builder.Services.AddSignalR();

// Add Infrastructure layer dependencies (DbContext, Repositories, Unit of Work)
builder.Services.AddInfrastructure(builder.Configuration);

// Register MediatR handlers from the Application layer assembly
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(PicksAndMore.Application.Products.Queries.GetProductsQuery).Assembly));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "P!cK5&m0R3#Jwt@S3cR3t-F4llB4ck_K3y$2026!xQz9vWpLm7nRtYu");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
        ClockSkew = TimeSpan.Zero
    };

    // Extract access token from query string for SignalR WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/notification-hub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure Dynamic Permission-Based Authorization
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

// Configure CORS: whitelist local dev + production Vercel frontend URL from environment
var allowedOrigins = new List<string> { "http://localhost:4200" };
var vercelUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
if (!string.IsNullOrEmpty(vercelUrl))
{
    allowedOrigins.Add(vercelUrl.TrimEnd('/'));
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Setup OpenAPI schema generation
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Picks & More API", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

// Configure development environment tools & seed database
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Picks & More API v1");
    });

    // Auto-seed database with default products and roles
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await DatabaseSeeder.SeedAsync(context);
            
            // Retroactively set image URLs if they are currently null/empty in database
            var productsToUpdate = await context.Products.ToListAsync();
            bool hasUpdates = false;
            foreach (var p in productsToUpdate)
            {
                if (string.IsNullOrEmpty(p.ImageUrl))
                {
                    if (p.Title.Contains("Handbag")) { p.ImageUrl = "/products/handbag.png"; hasUpdates = true; }
                    else if (p.Title.Contains("Heels")) { p.ImageUrl = "/products/heels.png"; hasUpdates = true; }
                    else if (p.Title.Contains("Satin Slip")) { p.ImageUrl = "/products/dress.png"; hasUpdates = true; }
                    else if (p.Title.Contains("Knit Dress")) { p.ImageUrl = "/products/dress.png"; hasUpdates = true; }
                    else if (p.Title.Contains("Organza Gown")) { p.ImageUrl = "/products/dress.png"; hasUpdates = true; }
                    else if (p.Title.Contains("Diaper Bag")) { p.ImageUrl = "/products/diaper_bag.png"; hasUpdates = true; }
                    else if (p.Title.Contains("Sneakers")) { p.ImageUrl = "/products/sneaker.png"; hasUpdates = true; }
                }
            }
            if (hasUpdates)
            {
                await context.SaveChangesAsync();
                Console.WriteLine("Retroactive product image URLs updated in database.");
            }

            // Ensure at least one product (e.g., Junior Active Sneakers) is On Sale
            var sneakerId = Guid.Parse("3e7748cd-24fa-4011-85f2-95fcd773d100");
            var sneaker = await context.Products.FirstOrDefaultAsync(p => p.Id == sneakerId);
            if (sneaker != null && sneaker.Price >= sneaker.CostPrice)
            {
                sneaker.Price = 80.00m;
                sneaker.CostPrice = 110.00m;
                await context.SaveChangesAsync();
                Console.WriteLine("Junior Active Sneakers updated to be On Sale.");
            }

            // Ensure a delivered order exists for Bestsellers testing
            if (!await context.Orders.AnyAsync())
            {
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@picksandmore.com");
                if (adminUser != null)
                {
                    var orderId = Guid.Parse("88888888-8888-8888-8888-888888888888");
                    var order = new Order(
                        orderId,
                        adminUser.Id,
                        DateTime.UtcNow.AddDays(-5),
                        3790.00m,
                        0.00m,
                        PicksAndMore.Domain.Enums.OrderStatus.Delivered,
                        PicksAndMore.Domain.Enums.PaymentMethod.COD,
                        new PicksAndMore.Domain.ValueObjects.Address("Cairo", "123 Nile St", "01002003004")
                    );

                    // Add items (e.g. Sculpted Leather Handbag, Stiletto Heels)
                    order.AddOrderItem(Guid.Parse("d3b07384-d113-40e1-a3f2-861f2113d077"), 3, 850.00m);
                    order.AddOrderItem(Guid.Parse("3e5c706d-e4c1-40e1-85f2-95fcd773d100"), 2, 620.00m);

                    await context.Orders.AddAsync(order);
                    await context.SaveChangesAsync();
                    Console.WriteLine("Seeded delivered order for Bestsellers.");
                }
            }

            Console.WriteLine("Database seeding check completed.");

        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}

// Only enforce HTTPS redirect in non-containerized environments (Render handles TLS termination)
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// Enable CORS mapping
app.UseCors("AllowAngular");

app.UseMiddleware<PicksAndMore.API.Middleware.VisitorTrackingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Map routing endpoints for Api Controllers
app.MapControllers();

// Map SignalR Notification Hub
app.MapHub<NotificationHub>("/api/notification-hub");

app.Run();
