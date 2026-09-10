using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Rate Limiting (MUST per Constitution on Auth endpoints)
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("AuthRateLimit", httpContext =>
            {
                var ip = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                      ?? httpContext.Connection.RemoteIpAddress?.ToString()
                      ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ip,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        // 2. CORS Policy for Frontend Development
        services.AddCors(options =>
        {
            options.AddPolicy("FrontendPolicy", policy =>
            {
                policy.SetIsOriginAllowed(_ => true) // Allows localhost origins for frontend dev servers
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        // 3. JWT Authentication
        var jwtSecret = configuration["Jwt:Secret"] ?? "default_development_secret_key_minimum_32_characters_long!";
        var key = Encoding.UTF8.GetBytes(jwtSecret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "RetailOS",
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"] ?? "RetailOS",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // 3. Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireOwner", policy => policy.RequireRole(Roles.Owner));
            options.AddPolicy("RequireManagerOrAbove", policy => policy.RequireRole(Roles.Owner, Roles.Manager));
            options.AddPolicy("RequireStaff", policy => policy.RequireRole(Roles.All.ToArray()));
        });

        // 4. Swagger with JWT Support
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Retail OS API",
                Version = "v1",
                Description = "Retail OS Modular Monolith Backend API"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
