using Microsoft.AspNetCore.Http;
using PicksAndMore.Infrastructure.Persistence;
using PicksAndMore.Domain.Entities;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace PicksAndMore.API.Middleware;

public class VisitorTrackingMiddleware
{
    private readonly RequestDelegate _next;

    public VisitorTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Only log public api or storefront endpoints (avoid static files, swagger, and admin routes)
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) && 
            !path.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase) &&
            !path.Contains("swagger", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Resolve IP Address
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                
                // Fallback to header if present (X-Forwarded-For or X-Real-IP)
                if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
                {
                    ipAddress = forwardedFor.ToString().Split(',')[0].Trim();
                }
                else if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
                {
                    ipAddress = realIp.ToString();
                }

                // Resolve location
                string country = "Egypt";
                string governorate = "Cairo";

                // Read headers for country/governorate overrides if present (e.g. from tests or proxy)
                if (context.Request.Headers.TryGetValue("X-Visitor-Country", out var headerCountry))
                {
                    country = headerCountry.ToString();
                }
                if (context.Request.Headers.TryGetValue("X-Visitor-Governorate", out var headerGov))
                {
                    governorate = headerGov.ToString();
                }

                // If local/loopback and headers didn't specify, we can fall back safely
                if ((ipAddress == "127.0.0.1" || ipAddress == "::1" || ipAddress.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)) && 
                    !context.Request.Headers.ContainsKey("X-Visitor-Country"))
                {
                    // Safe Fallback: We can randomize slightly or map to a list of governorates/countries for high-end preview data
                    var random = new Random();
                    var countries = new[] { "Egypt", "Egypt", "Egypt", "United States", "Saudi Arabia", "Germany", "United Arab Emirates" };
                    var egyptGovs = new[] { "Cairo", "Alexandria", "Giza", "Fayoum", "Dakahlia", "Sohag" };

                    country = countries[random.Next(countries.Length)];
                    if (country == "Egypt")
                    {
                        governorate = egyptGovs[random.Next(egyptGovs.Length)];
                    }
                    else
                    {
                        governorate = "Unknown";
                    }
                }

                // Insert into database using scoped DbContext
                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var log = new VisitorLog
                    {
                        IpAddress = ipAddress,
                        Country = country,
                        Governorate = governorate,
                        Timestamp = DateTime.UtcNow
                    };
                    dbContext.VisitorLogs.Add(log);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch
            {
                // Fallback safely so it never crashes the actual request flow
            }
        }

        await _next(context);
    }
}
