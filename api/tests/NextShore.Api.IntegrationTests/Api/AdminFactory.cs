using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace NextShore.Api.IntegrationTests.Api;

/// <summary>
/// <see cref="ApiFactory"/> whose JWT bearer trusts a local signing key in place of Clerk's, so tests can mint
/// session tokens like the ones Clerk issues (issuer = authority, <c>azp</c> = web origin, custom <c>role</c> claim).
/// </summary>
public class AdminFactory : ApiFactory
{
    public const string Issuer = "https://clerk.test.nextshore";
    public const string WebOrigin = "https://web.test.nextshore";

    private static readonly SymmetricSecurityKey SigningKey = new(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Clerk:Authority", Issuer);
        builder.UseSetting("Cors:Origins", WebOrigin);
        base.ConfigureWebHost(builder);
    }

    protected override void ConfigureServices(IServiceCollection services) =>
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = null;
            options.ConfigurationManager = null;
            options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration { Issuer = Issuer };
            options.TokenValidationParameters.IssuerSigningKey = SigningKey;
        });

    public static string Token(
        string? role = "admin", string issuer = Issuer, string? azp = WebOrigin, DateTime? expires = null, string sub = "user_admin")
    {
        var claims = new List<Claim> { new("sub", sub) };
        if (role is not null)
        {
            claims.Add(new Claim("role", role));
        }

        if (azp is not null)
        {
            claims.Add(new Claim("azp", azp));
        }

        var expiry = expires ?? DateTime.UtcNow.AddMinutes(5);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Subject = new ClaimsIdentity(claims),
            NotBefore = expiry.AddMinutes(-10),
            Expires = expiry,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        });
    }

    public HttpClient CreateAdminClient(string? token = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token ?? Token());
        return client;
    }
}
