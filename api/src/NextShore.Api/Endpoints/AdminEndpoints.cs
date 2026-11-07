using System.Security.Claims;
using FluentValidation;
using Microsoft.Extensions.Options;
using NextShore.Api.Setup;
using NextShore.Application.Admin;
using NextShore.Application.Engine;
using NextShore.Domain;

namespace NextShore.Api.Endpoints;

public sealed record AdminMeResponse(string UserId, string Role);

/// <summary>
/// <c>/admin</c> (docs/API.md). Writes go to drafts and return the pathway's working view; publish makes them live.
/// </summary>
public static class AdminEndpoints
{
    private const int MaxCsvBytes = 1024 * 1024;

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin")
            .RequireAuthorization(AuthPolicies.Admin)
            .WithTags("Admin")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        admin.MapGet("/me", (ClaimsPrincipal user) =>
                TypedResults.Ok(new AdminMeResponse(Actor(user), user.FindFirstValue(AuthSetup.RoleClaim)!)))
            .WithName("GetAdminMe")
            .WithSummary("The signed-in admin (checks the token and role)");

        // ---- pathways

        admin.MapGet("/pathways", async (IRuleAdmin rules, CancellationToken ct) => TypedResults.Ok(await rules.ListPathwaysAsync(ct)))
            .WithName("AdminListPathways")
            .WithSummary("All pathways incl. drafts, with stale and publish-blocker counts");

        admin.MapGet("/pathways/{id:guid}", async (Guid id, IRuleAdmin rules, CancellationToken ct) =>
                Respond(await rules.GetPathwayAsync(id, ct)))
            .WithName("AdminGetPathway")
            .WithSummary("Working view of a pathway: its draft when one exists, else the live rows")
            .Produces<AdminPathwayResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/pathways/{id:guid}", (Guid id, PathwayUpdateRequest request, IValidator<PathwayUpdateRequest> validator,
                    IRuleAdmin rules, ClaimsPrincipal user, CancellationToken ct) =>
                Validated(request, validator, ct, () => rules.UpdatePathwayAsync(id, request, Actor(user), ct)))
            .WithName("AdminUpdatePathway")
            .WithSummary("Edit pathway fields (in its draft)")
            .Produces<AdminPathwayResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/pathways/{id:guid}/draft", async (Guid id, IRuleAdmin rules, ClaimsPrincipal user, CancellationToken ct) =>
                Respond(await rules.DiscardDraftAsync(id, Actor(user), ct)))
            .WithName("AdminDiscardDraft")
            .WithSummary("Discard a published pathway's unpublished changes")
            .Produces<AdminPathwayResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admin.MapPost("/pathways/{id:guid}/publish", async (Guid id, IRuleAdmin rules, ClaimsPrincipal user, CancellationToken ct) =>
                Respond(await rules.PublishAsync(id, Actor(user), ct)))
            .WithName("AdminPublishPathway")
            .WithSummary("Publish the pathway's draft; 409 while any row is unverified")
            .Produces<PublishResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // ---- requirements

        admin.MapGet("/pathways/{id:guid}/requirements", async (Guid id, IRuleAdmin rules, CancellationToken ct) =>
                Respond(await rules.GetPathwayAsync(id, ct), p => p.Requirements))
            .WithName("AdminListRequirements")
            .Produces<IReadOnlyList<AdminRequirementResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPost("/pathways/{id:guid}/requirements", (Guid id, RequirementRequest request, IValidator<RequirementRequest> validator,
                    IRuleAdmin rules, ClaimsPrincipal user, CancellationToken ct) =>
                Validated(request, validator, ct, () => rules.AddRequirementAsync(id, request, Actor(user), ct)))
            .WithName("AdminAddRequirement")
            .Produces<AdminPathwayResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/requirements/{id:guid}", (Guid id, RequirementRequest request, IValidator<RequirementRequest> validator,
                    IRuleAdmin rules, ClaimsPrincipal user, CancellationToken ct) =>
                Validated(request, validator, ct, () => rules.UpdateRequirementAsync(id, request, Actor(user), ct)))
            .WithName("AdminUpdateRequirement")
            .Produces<AdminPathwayResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/requirements/{id:guid}", async (Guid id, IRuleAdmin rules, ClaimsPrincipal user, CancellationToken ct) =>
                Respond(await rules.DeleteRequirementAsync(id, Actor(user), ct)))
            .WithName("AdminDeleteRequirement")
            .Produces<AdminPathwayResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- points factors

        admin.MapGet("/pathways/{id:guid}/points-factors", async (Guid id, IRuleAdmin rules, CancellationToken ct) =>
                Respond(await rules.GetPathwayAsync(id, ct), p => p.PointsFactors))
            .WithName("AdminListPointsFactors")
            .Produces<IReadOnlyList<AdminPointsFactorResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPost("/pathways/{id:guid}/points-factors", (Guid id, PointsFactorRequest request, IValidator<PointsFactorRequest> validator,
                    IRuleAdmin rules, ClaimsPrincipal user, CancellationToken ct) =>
                Validated(request, validator, ct, () => rules.AddPointsFactorAsync(id, request, Actor(user), ct)))
            .WithName("AdminAddPointsFactor")
            .Produces<AdminPathwayResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/points-factors/{id:guid}", (Guid id, PointsFactorRequest request, IValidator<PointsFactorRequest> validator,
                    IRuleAdmin rules, ClaimsPrincipal user, CancellationToken ct) =>
                Validated(request, validator, ct, () => rules.UpdatePointsFactorAsync(id, request, Actor(user), ct)))
            .WithName("AdminUpdatePointsFactor")
            .Produces<AdminPathwayResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/points-factors/{id:guid}", async (Guid id, IRuleAdmin rules, ClaimsPrincipal user, CancellationToken ct) =>
                Respond(await rules.DeletePointsFactorAsync(id, Actor(user), ct)))
            .WithName("AdminDeletePointsFactor")
            .Produces<AdminPathwayResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- occupations

        admin.MapGet("/pathways/{id:guid}/occupations", async (Guid id, IRuleAdmin rules, CancellationToken ct) =>
                Respond(await rules.GetPathwayAsync(id, ct), p => p.Occupations))
            .WithName("AdminListOccupations")
            .Produces<IReadOnlyList<AdminOccupationResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPost("/pathways/{id:guid}/occupations", async (Guid id, HttpRequest http, IRuleAdmin rules, ClaimsPrincipal user,
                    CancellationToken ct) =>
                {
                    if (http.ContentLength > MaxCsvBytes)
                    {
                        return TypedResults.Problem(statusCode: StatusCodes.Status413PayloadTooLarge, title: "CSV is over 1 MB");
                    }

                    using var reader = new StreamReader(http.Body);
                    var parsed = OccupationCsv.Parse(await reader.ReadToEndAsync(ct));
                    return parsed.IsSuccess
                        ? Respond(await rules.ImportOccupationsAsync(id, parsed.Value, Actor(user), ct))
                        : parsed.Error!.ToProblem();
                })
            .Accepts<string>("text/csv")
            .WithName("AdminImportOccupations")
            .WithSummary("Upsert occupations by code from CSV: code,title,is_eligible[,source_url,verified_at]")
            .Produces<OccupationImportResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/occupations/{id:guid}", async (Guid id, IRuleAdmin rules, ClaimsPrincipal user, CancellationToken ct) =>
                Respond(await rules.DeleteOccupationAsync(id, Actor(user), ct)))
            .WithName("AdminDeleteOccupation")
            .Produces<AdminPathwayResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- language mappings, stale rules, audit

        admin.MapGet("/language-mappings", async (IRuleAdmin rules, CancellationToken ct) =>
                TypedResults.Ok(await rules.GetLanguageMappingsAsync(ct)))
            .WithName("AdminLanguageMappings")
            .WithSummary("Counts of published, publishable and blocked language score mappings");

        admin.MapPost("/language-mappings/publish", async (IRuleAdmin rules, ClaimsPrincipal user, CancellationToken ct) =>
                TypedResults.Ok(await rules.PublishLanguageMappingsAsync(Actor(user), ct)))
            .WithName("AdminPublishLanguageMappings")
            .WithSummary("Publish every verified draft language mapping");

        admin.MapGet("/rules/stale", async (int? days, IOptions<EngineSettings> engine, IRuleAdmin rules, CancellationToken ct) =>
                days is < 0
                    ? Invalid("days", "Must be zero or more.")
                    : TypedResults.Ok(await rules.ListStaleAsync(days ?? engine.Value.StaleAfterDays, ct)))
            .WithName("AdminStaleRules")
            .WithSummary("Published rules verified more than `days` (default Engine:StaleAfterDays) ago")
            .Produces<IReadOnlyList<StaleRuleResponse>>()
            .ProducesValidationProblem();

        admin.MapGet("/audit", async (string? entity, Guid? pathwayId, DateTimeOffset? from, int? page, int? pageSize,
                    IRuleAdmin rules, CancellationToken ct) =>
                TypedResults.Ok(await rules.ListAuditAsync(new AuditQuery(entity, pathwayId, from, page ?? 1, pageSize ?? 50), ct)))
            .WithName("AdminAudit")
            .WithSummary("Audit log, newest first; filter by table, pathway and start time; paged (max 200)");

        return admin;
    }

    /// <summary>Clerk user id (<c>sub</c>), recorded as the audit actor.</summary>
    private static string Actor(ClaimsPrincipal user) => user.Identity?.Name ?? "unknown";

    private static IResult Respond<T>(Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error!.ToProblem();

    private static IResult Respond<T, TOut>(Result<T> result, Func<T, TOut> select) =>
        result.IsSuccess ? TypedResults.Ok(select(result.Value)) : result.Error!.ToProblem();

    private static async Task<IResult> Validated<T, TOut>(
        T request, IValidator<T> validator, CancellationToken ct, Func<Task<Result<TOut>>> action)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Error.Validation(validation.ToDictionary().AsReadOnly()).ToProblem();
        }

        return Respond(await action());
    }

    private static IResult Invalid(string field, string message) =>
        Error.Validation(new Dictionary<string, string[]> { [field] = [message] }).ToProblem();
}
