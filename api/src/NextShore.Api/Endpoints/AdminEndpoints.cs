using System.Security.Claims;
using NextShore.Api.Setup;

namespace NextShore.Api.Endpoints;

public sealed record AdminMeResponse(string UserId, string Role);

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin")
            .RequireAuthorization(AuthPolicies.Admin)
            .WithTags("Admin");

        admin.MapGet("/me", (ClaimsPrincipal user) =>
                TypedResults.Ok(new AdminMeResponse(user.Identity!.Name!, user.FindFirstValue(AuthSetup.RoleClaim)!)))
            .WithName("GetAdminMe")
            .WithSummary("The signed-in admin (checks the token and role)");

        return admin;
    }
}
