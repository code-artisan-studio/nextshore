using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace NextShore.Api.Setup;

public static class AuthPolicies
{
    /// <summary>A Clerk session token whose <c>role</c> claim is <c>admin</c>.</summary>
    public const string Admin = "admin";
}

/// <summary>
/// Clerk issues the admin's session token (a JWT signed with the instance's keys); public endpoints stay anonymous.
/// <c>Clerk:Authority</c> is the Clerk Frontend API URL, which is also the token issuer. The role comes from a custom
/// session-token claim, <c>"role": "{{user.public_metadata.role}}"</c>, set in the Clerk dashboard.
/// </summary>
public static class AuthSetup
{
    public const string RoleClaim = "role";
    public const string AdminRole = "admin";

    public static IServiceCollection AddClerkAuth(this IServiceCollection services, IConfiguration config)
    {
        var authority = config["Clerk:Authority"]?.TrimEnd('/');
        var origins = CorsSetup.Origins(config);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Without an authority no signing keys are known, so every admin request is 401.
                if (!string.IsNullOrWhiteSpace(authority))
                {
                    options.Authority = authority;
                }

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    // Clerk session tokens carry no `aud`; `azp` (the page's origin) is checked below instead.
                    ValidateAudience = false,
                    NameClaimType = "sub",
                    RoleClaimType = RoleClaim,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var azp = context.Principal?.FindFirst("azp")?.Value;
                        if (azp is not null && !origins.Contains(azp.TrimEnd('/'), StringComparer.OrdinalIgnoreCase))
                        {
                            context.Fail("Token was issued for another origin.");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.Admin, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(RoleClaim, AdminRole));

        return services;
    }

    /// <summary>A missing <c>Clerk__Authority</c> only shows up as 401s in /admin; say so at startup.</summary>
    public static void WarnIfNoClerkAuthority(this WebApplication app)
    {
        if (string.IsNullOrWhiteSpace(app.Configuration["Clerk:Authority"]))
        {
            app.Logger.LogWarning("No Clerk authority configured (Clerk__Authority): every admin request will be 401.");
        }
    }
}
