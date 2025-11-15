using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Security;

public static class AddJwtValidation
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var authConfig = config.GetSection("Authentication").Get<AuthenticationConfig>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = authConfig!.Authority;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authConfig.ValidIssuer,
                ValidateAudience = true,
                ValidAudience = authConfig.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is not ClaimsIdentity claimsIdentity) return Task.CompletedTask;

                    var resourceAccess = context?.Principal?.FindFirst("resource_access")?.Value;                      
                    if (!string.IsNullOrWhiteSpace(resourceAccess))
                    {
                        try
                        {
                            var parsed = JsonNode.Parse(resourceAccess);
                            var clientId = authConfig.ClientId;
                            var roles = parsed?[clientId]?["roles"]?.AsArray();
                            if (roles != null)
                            {
                                foreach (var r in roles)
                                {
                                    var roleName = r?.ToString();
                                    if (!string.IsNullOrWhiteSpace(roleName) && !claimsIdentity.HasClaim(ClaimTypes.Role, roleName))
                                    {
                                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    var realmAccess = context?.Principal?.FindFirst("realm_access")?.Value;
                    if (!string.IsNullOrWhiteSpace(realmAccess))
                    {
                        try
                        {
                            var parsedRealm = JsonNode.Parse(realmAccess);
                            var realmRoles = parsedRealm?["roles"]?.AsArray();
                            if (realmRoles != null)
                            {
                                foreach (var r in realmRoles)
                                {
                                    var roleName = r?.ToString();
                                    if (!string.IsNullOrWhiteSpace(roleName) && !claimsIdentity.HasClaim(ClaimTypes.Role, roleName))
                                    {
                                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    
                    return Task.CompletedTask;
                }
            };
        });
        return services;
    }

    public static IServiceCollection AddAuthorizationWithRoles(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("AdminPolicy", policy =>
                policy.Requirements.Add(new RoleRequirement(UserRole.Admin)))
            .AddPolicy("EmployeePolicy", policy => 
                policy.Requirements.Add(new RoleRequirement(UserRole.Employee)))
            .AddPolicy("CustomerPolicy", policy =>
                policy.Requirements.Add(new RoleRequirement(UserRole.Customer)));
        
        services.AddSingleton<IAuthorizationHandler, RoleRequirementHandler>();
        
        return services;
    }
}