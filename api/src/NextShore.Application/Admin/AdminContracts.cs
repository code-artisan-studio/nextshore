using System.Text.Json;
using NextShore.Domain;

namespace NextShore.Application.Admin;

// ---- Requests. Enum-like values are the API's snake_case text; unverified fields are snake_case column names. ----

public sealed record PathwayUpdateRequest(
    string Name,
    string Type,
    IReadOnlyList<string> Audience,
    bool IsPointsBased,
    int? PassMark,
    int? Cutoff,
    DateOnly? CutoffAsOf,
    Guid? NextPathwayId,
    int? TypicalDurationMonths,
    string Summary,
    string SourceUrl,
    DateOnly? VerifiedAt,
    IReadOnlyList<string> UnverifiedFields);

/// <param name="Value">Scalar or array; null only for <c>exists</c> / <c>true</c> or while <c>value</c> is unverified.</param>
public sealed record RequirementRequest(
    string Field,
    string Operator,
    JsonElement? Value,
    string? Unit,
    bool IsMandatory,
    string Fixability,
    string FixHint,
    int? FixMonths,
    string Label,
    string SourceUrl,
    DateOnly? VerifiedAt,
    IReadOnlyList<string> UnverifiedFields);

public sealed record PointsFactorRequest(
    string Factor,
    string Variant,
    decimal? Min,
    decimal? Max,
    string? MatchValue,
    int? Points,
    int? MaxPointsForFactor,
    string SourceUrl,
    DateOnly? VerifiedAt,
    IReadOnlyList<string> UnverifiedFields);

/// <summary>One parsed line of an occupations CSV import.</summary>
public sealed record OccupationRow(int Line, string Code, string Title, bool IsEligible, string? SourceUrl, DateOnly? VerifiedAt);

public sealed record AuditQuery(string? Entity, Guid? PathwayId, DateTimeOffset? From, int Page = 1, int PageSize = 50);

// ---- Responses. Pathway detail is the working view: the draft copy when one exists, else the live rows. ----

public sealed record PublishBlockerResponse(string Entity, string Label, string Reason);

/// <param name="Status"><c>published</c> (live; edits go to a draft copy) or <c>draft</c> (never published).</param>
/// <param name="HasDraft">A published pathway with unpublished changes.</param>
/// <param name="StaleRules">Working rows verified more than <c>Engine:StaleAfterDays</c> ago.</param>
public sealed record AdminPathwayListItem(
    Guid Id,
    string Slug,
    string Name,
    string CountryCode,
    string Type,
    string Status,
    bool HasDraft,
    int RuleVersion,
    DateOnly? VerifiedAt,
    int StaleRules,
    int PublishBlockers);

public sealed record AdminRequirementResponse(
    Guid Id,
    string Status,
    string Field,
    string Operator,
    JsonElement? Value,
    string? Unit,
    bool IsMandatory,
    string Fixability,
    string FixHint,
    int? FixMonths,
    string Label,
    string SourceUrl,
    DateOnly? VerifiedAt,
    IReadOnlyList<string> UnverifiedFields,
    bool Stale);

public sealed record AdminPointsFactorResponse(
    Guid Id,
    string Status,
    string Factor,
    string Variant,
    decimal? Min,
    decimal? Max,
    string? MatchValue,
    int? Points,
    int? MaxPointsForFactor,
    string SourceUrl,
    DateOnly? VerifiedAt,
    IReadOnlyList<string> UnverifiedFields,
    bool Stale);

public sealed record AdminOccupationResponse(
    Guid Id,
    string Status,
    string Code,
    string Title,
    bool IsEligible,
    string SourceUrl,
    DateOnly? VerifiedAt,
    IReadOnlyList<string> UnverifiedFields,
    bool Stale);

/// <param name="Id">The pathway's stable id (the live row), also while a draft copy is being edited.</param>
public sealed record AdminPathwayResponse(
    Guid Id,
    string Slug,
    string CountryCode,
    string Status,
    bool HasDraft,
    int RuleVersion,
    string Name,
    string Type,
    IReadOnlyList<string> Audience,
    bool IsPointsBased,
    int? PassMark,
    int? Cutoff,
    DateOnly? CutoffAsOf,
    Guid? NextPathwayId,
    int? TypicalDurationMonths,
    string Summary,
    string SourceUrl,
    DateOnly? VerifiedAt,
    IReadOnlyList<string> UnverifiedFields,
    bool Stale,
    IReadOnlyList<AdminRequirementResponse> Requirements,
    IReadOnlyList<AdminPointsFactorResponse> PointsFactors,
    IReadOnlyList<AdminOccupationResponse> Occupations,
    IReadOnlyList<PublishBlockerResponse> PublishBlockers);

public sealed record OccupationImportResponse(int Created, int Updated, AdminPathwayResponse Pathway);

public sealed record PublishResponse(Guid Id, string Slug, int RuleVersion);

public sealed record StaleRuleResponse(
    string Entity, Guid Id, Guid? PathwayId, string? PathwaySlug, string Label, DateOnly VerifiedAt, int DaysSinceVerified);

public sealed record AuditEntryResponse(
    Guid Id,
    string Entity,
    Guid EntityId,
    Guid? PathwayId,
    string Action,
    JsonElement? OldValue,
    JsonElement? NewValue,
    string Actor,
    DateTimeOffset At);

public sealed record AuditPageResponse(IReadOnlyList<AuditEntryResponse> Items, int Page, int PageSize, int Total);

/// <param name="DraftReady">Draft mappings that pass <c>PublishGuard</c>.</param>
public sealed record LanguageMappingsStatusResponse(int Published, int DraftReady, int DraftBlocked);

/// <summary>
/// Admin rule editing (docs/ARCHITECTURE.md §5). Writes never touch live rows: a published pathway is edited through a
/// draft copy of its whole tree, which publish applies. Every write is audited with <paramref name="actor"/>.
/// </summary>
public interface IRuleAdmin
{
    Task<IReadOnlyList<AdminPathwayListItem>> ListPathwaysAsync(CancellationToken cancellationToken);

    Task<Result<AdminPathwayResponse>> GetPathwayAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<AdminPathwayResponse>> UpdatePathwayAsync(Guid id, PathwayUpdateRequest request, string actor, CancellationToken cancellationToken);

    Task<Result<AdminPathwayResponse>> AddRequirementAsync(Guid pathwayId, RequirementRequest request, string actor, CancellationToken cancellationToken);

    Task<Result<AdminPathwayResponse>> UpdateRequirementAsync(Guid id, RequirementRequest request, string actor, CancellationToken cancellationToken);

    Task<Result<AdminPathwayResponse>> DeleteRequirementAsync(Guid id, string actor, CancellationToken cancellationToken);

    Task<Result<AdminPathwayResponse>> AddPointsFactorAsync(Guid pathwayId, PointsFactorRequest request, string actor, CancellationToken cancellationToken);

    Task<Result<AdminPathwayResponse>> UpdatePointsFactorAsync(Guid id, PointsFactorRequest request, string actor, CancellationToken cancellationToken);

    Task<Result<AdminPathwayResponse>> DeletePointsFactorAsync(Guid id, string actor, CancellationToken cancellationToken);

    /// <summary>Upserts occupations by code into the working tree.</summary>
    Task<Result<OccupationImportResponse>> ImportOccupationsAsync(
        Guid pathwayId, IReadOnlyList<OccupationRow> rows, string actor, CancellationToken cancellationToken);

    Task<Result<AdminPathwayResponse>> DeleteOccupationAsync(Guid id, string actor, CancellationToken cancellationToken);

    /// <summary>Drops a published pathway's draft copy.</summary>
    Task<Result<AdminPathwayResponse>> DiscardDraftAsync(Guid pathwayId, string actor, CancellationToken cancellationToken);

    /// <summary>Refused (409) while <c>PublishGuard</c> reports blockers or there is nothing to publish.</summary>
    Task<Result<PublishResponse>> PublishAsync(Guid pathwayId, string actor, CancellationToken cancellationToken);

    /// <summary>Published rows verified more than <paramref name="days"/> days ago, oldest first.</summary>
    Task<IReadOnlyList<StaleRuleResponse>> ListStaleAsync(int days, CancellationToken cancellationToken);

    Task<AuditPageResponse> ListAuditAsync(AuditQuery query, CancellationToken cancellationToken);

    Task<LanguageMappingsStatusResponse> GetLanguageMappingsAsync(CancellationToken cancellationToken);

    /// <summary>Publishes every draft language mapping that passes <c>PublishGuard</c>; blocked ones stay draft.</summary>
    Task<LanguageMappingsStatusResponse> PublishLanguageMappingsAsync(string actor, CancellationToken cancellationToken);
}
