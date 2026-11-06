using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextShore.Application.Abstractions;
using NextShore.Application.Admin;
using NextShore.Application.Common;
using NextShore.Application.Engine;
using NextShore.Domain;
using NextShore.Domain.Entities;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;
using NextShore.Infrastructure.Persistence;
using NextShore.Infrastructure.Rules;

namespace NextShore.Infrastructure.Admin;

/// <summary>
/// Draft model: a pathway's <em>live</em> row (replaces_id null) keeps a stable id. A never-published pathway is
/// edited in place (it is the draft). The first write to a published pathway creates a draft copy of the whole tree
/// (pathway + children, each with replaces_id = the row it copies); publish then copies the draft's pathway fields onto
/// the live row, deletes the live children, re-parents the draft children onto the live row and drops the copy.
/// Live rules therefore never change until publish, and publish is one transaction.
/// </summary>
public sealed class RuleAdmin(
    NextShoreDbContext db,
    IRuleRepository rules,
    TimeProvider clock,
    IOptions<EngineSettings> engine,
    ILogger<RuleAdmin> logger) : IRuleAdmin
{
    private const string PathwaysTable = "pathways";
    private const string RequirementsTable = "requirements";
    private const string PointsFactorsTable = "points_factors";
    private const string OccupationsTable = "occupation_lists";
    private const string LanguageMappingsTable = "language_score_mappings";

    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    private DateOnly StaleBefore => Today.AddDays(-engine.Value.StaleAfterDays);

    // ---------------------------------------------------------------- reads

    public async Task<IReadOnlyList<AdminPathwayListItem>> ListPathwaysAsync(CancellationToken ct)
    {
        var all = await Trees().AsNoTracking().ToListAsync(ct);
        var copies = all.Where(p => p.ReplacesId is not null).ToDictionary(p => p.ReplacesId!.Value);
        return all.Where(p => p.ReplacesId is null)
            .OrderBy(p => p.Country!.Code).ThenBy(p => p.Slug, StringComparer.Ordinal)
            .Select(live =>
            {
                var working = copies.GetValueOrDefault(live.Id) ?? live;
                return new AdminPathwayListItem(
                    live.Id, live.Slug, working.Name, live.Country!.Code, EnumText.Of(working.Type), EnumText.Of(live.Status),
                    copies.ContainsKey(live.Id), live.RuleVersion, working.VerifiedAt, Rows(working).Count(IsStale),
                    PublishGuard.Check(working).Count);
            })
            .ToList();
    }

    public async Task<Result<AdminPathwayResponse>> GetPathwayAsync(Guid id, CancellationToken ct) =>
        await LoadAsync(id, ct) is { } tree ? ToResponse(tree) : NotFound(id);

    public async Task<IReadOnlyList<StaleRuleResponse>> ListStaleAsync(int days, CancellationToken ct)
    {
        var before = Today.AddDays(-days);
        var live = await Trees().AsNoTracking()
            .Where(p => p.ReplacesId == null && p.Status == RuleStatus.Published)
            .ToListAsync(ct);
        var mappings = await db.LanguageScoreMappings.AsNoTracking()
            .Where(m => m.Status == RuleStatus.Published && m.VerifiedAt < before)
            .ToListAsync(ct);

        var stale = new List<StaleRuleResponse>();
        foreach (var p in live)
        {
            void Add(string entity, SourcedRuleEntity row, string label)
            {
                if (row.Status == RuleStatus.Published && row.VerifiedAt is { } at && at < before)
                {
                    stale.Add(new(entity, row.Id, p.Id, p.Slug, label, at, Today.DayNumber - at.DayNumber));
                }
            }

            Add(PathwaysTable, p, p.Name);
            p.Requirements.ForEach(r => Add(RequirementsTable, r, r.Label));
            p.PointsFactors.ForEach(f => Add(PointsFactorsTable, f, FactorLabel(f)));
            p.Occupations.ForEach(o => Add(OccupationsTable, o, $"{o.Code} {o.Title}"));
        }

        stale.AddRange(mappings.Select(m => new StaleRuleResponse(
            LanguageMappingsTable, m.Id, null, null, MappingLabel(m), m.VerifiedAt!.Value, Today.DayNumber - m.VerifiedAt.Value.DayNumber)));
        return stale.OrderBy(s => s.VerifiedAt).ThenBy(s => s.PathwaySlug).ThenBy(s => s.Label).ToList();
    }

    public async Task<AuditPageResponse> ListAuditAsync(AuditQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 200);
        var rows = db.RuleAuditLog.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Entity)) rows = rows.Where(a => a.Entity == query.Entity);
        if (query.PathwayId is { } pathwayId) rows = rows.Where(a => a.PathwayId == pathwayId);
        if (query.From is { } from) rows = rows.Where(a => a.At >= from);

        var total = await rows.CountAsync(ct);
        var items = await rows.OrderByDescending(a => a.At).ThenByDescending(a => a.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new AuditPageResponse(items.Select(a => new AuditEntryResponse(
            a.Id, a.Entity, a.EntityId, a.PathwayId, EnumText.Of(a.Action), ParseJson(a.OldValue), ParseJson(a.NewValue),
            a.Actor, a.At)).ToList(), page, size, total);
    }

    public async Task<LanguageMappingsStatusResponse> GetLanguageMappingsAsync(CancellationToken ct)
    {
        var mappings = await db.LanguageScoreMappings.AsNoTracking().ToListAsync(ct);
        return MappingStatus(mappings);
    }

    // ---------------------------------------------------------------- pathway + children writes

    public Task<Result<AdminPathwayResponse>> UpdatePathwayAsync(Guid id, PathwayUpdateRequest request, string actor, CancellationToken ct) =>
        WriteAsync(id, actor, ct, async (tree, working) =>
        {
            if (request.NextPathwayId is { } next)
            {
                if (next == tree.Live.Id)
                {
                    return Invalid("nextPathwayId", "A pathway can't lead to itself.");
                }

                if (!await db.Pathways.AnyAsync(p => p.Id == next && p.ReplacesId == null, ct))
                {
                    return Invalid("nextPathwayId", "Unknown pathway.");
                }
            }

            var old = PathwayFields(working);
            working.Name = request.Name.Trim();
            working.Type = Parse<PathwayType>(request.Type);
            working.Audience = request.Audience.Distinct().Select(Parse<Audience>).ToList();
            working.IsPointsBased = request.IsPointsBased;
            working.PassMark = request.PassMark;
            working.Cutoff = request.Cutoff;
            working.CutoffAsOf = request.CutoffAsOf;
            working.NextPathwayId = request.NextPathwayId;
            working.TypicalDurationMonths = request.TypicalDurationMonths;
            working.Summary = request.Summary.Trim();
            SetSource(working, request.SourceUrl, request.VerifiedAt, request.UnverifiedFields);
            Touch(working);
            Audit(PathwaysTable, working.Id, tree.Live.Id, AuditAction.Update, old, PathwayFields(working), actor);
            return null;
        });

    public Task<Result<AdminPathwayResponse>> AddRequirementAsync(Guid pathwayId, RequirementRequest request, string actor, CancellationToken ct) =>
        WriteAsync(pathwayId, actor, ct, (tree, working) =>
        {
            var row = new Requirement { Id = Guid.NewGuid(), Field = "", FixHint = "", Label = "", SourceUrl = "", CreatedAt = Now };
            Apply(row, request);
            db.Requirements.Add(row); // explicit: a preset key on a navigation-added row reads as an update
            working.Requirements.Add(row);
            Audit(RequirementsTable, row.Id, tree.Live.Id, AuditAction.Create, null, ToResponse(row), actor);
            return Task.FromResult<Error?>(null);
        });

    public Task<Result<AdminPathwayResponse>> UpdateRequirementAsync(Guid id, RequirementRequest request, string actor, CancellationToken ct) =>
        WriteChildAsync(id, db.Requirements, w => w.Requirements, actor, ct, (tree, row) =>
        {
            var old = ToResponse(row);
            Apply(row, request);
            Audit(RequirementsTable, row.Id, tree.Live.Id, AuditAction.Update, old, ToResponse(row), actor);
        });

    public Task<Result<AdminPathwayResponse>> DeleteRequirementAsync(Guid id, string actor, CancellationToken ct) =>
        WriteChildAsync(id, db.Requirements, w => w.Requirements, actor, ct, (tree, row) =>
        {
            db.Requirements.Remove(row);
            Audit(RequirementsTable, row.Id, tree.Live.Id, AuditAction.Delete, ToResponse(row), null, actor);
        });

    public Task<Result<AdminPathwayResponse>> AddPointsFactorAsync(Guid pathwayId, PointsFactorRequest request, string actor, CancellationToken ct) =>
        WriteAsync(pathwayId, actor, ct, (tree, working) =>
        {
            var row = new PointsFactor { Id = Guid.NewGuid(), Factor = "", SourceUrl = "", CreatedAt = Now };
            Apply(row, request);
            db.PointsFactors.Add(row);
            working.PointsFactors.Add(row);
            Audit(PointsFactorsTable, row.Id, tree.Live.Id, AuditAction.Create, null, ToResponse(row), actor);
            return Task.FromResult<Error?>(null);
        });

    public Task<Result<AdminPathwayResponse>> UpdatePointsFactorAsync(Guid id, PointsFactorRequest request, string actor, CancellationToken ct) =>
        WriteChildAsync(id, db.PointsFactors, w => w.PointsFactors, actor, ct, (tree, row) =>
        {
            var old = ToResponse(row);
            Apply(row, request);
            Audit(PointsFactorsTable, row.Id, tree.Live.Id, AuditAction.Update, old, ToResponse(row), actor);
        });

    public Task<Result<AdminPathwayResponse>> DeletePointsFactorAsync(Guid id, string actor, CancellationToken ct) =>
        WriteChildAsync(id, db.PointsFactors, w => w.PointsFactors, actor, ct, (tree, row) =>
        {
            db.PointsFactors.Remove(row);
            Audit(PointsFactorsTable, row.Id, tree.Live.Id, AuditAction.Delete, ToResponse(row), null, actor);
        });

    public async Task<Result<OccupationImportResponse>> ImportOccupationsAsync(
        Guid pathwayId, IReadOnlyList<OccupationRow> rows, string actor, CancellationToken ct)
    {
        int created = 0, updated = 0;
        var result = await WriteAsync(pathwayId, actor, ct, (tree, working) =>
        {
            var byCode = working.Occupations.ToDictionary(o => o.Code, StringComparer.Ordinal);
            foreach (var r in rows)
            {
                if (byCode.TryGetValue(r.Code, out var row))
                {
                    var old = ToResponse(row);
                    Apply(row, r, working.SourceUrl);
                    Audit(OccupationsTable, row.Id, tree.Live.Id, AuditAction.Update, old, ToResponse(row), actor);
                    updated++;
                }
                else
                {
                    row = new OccupationListEntry { Id = Guid.NewGuid(), Code = r.Code, Title = "", SourceUrl = "", CreatedAt = Now };
                    Apply(row, r, working.SourceUrl);
                    db.OccupationLists.Add(row);
                    working.Occupations.Add(row);
                    Audit(OccupationsTable, row.Id, tree.Live.Id, AuditAction.Create, null, ToResponse(row), actor);
                    created++;
                }
            }

            return Task.FromResult<Error?>(null);
        });
        return result.IsSuccess ? new OccupationImportResponse(created, updated, result.Value) : result.Error!;
    }

    public Task<Result<AdminPathwayResponse>> DeleteOccupationAsync(Guid id, string actor, CancellationToken ct) =>
        WriteChildAsync(id, db.OccupationLists, w => w.Occupations, actor, ct, (tree, row) =>
        {
            db.OccupationLists.Remove(row);
            Audit(OccupationsTable, row.Id, tree.Live.Id, AuditAction.Delete, ToResponse(row), null, actor);
        });

    public async Task<Result<AdminPathwayResponse>> DiscardDraftAsync(Guid pathwayId, string actor, CancellationToken ct)
    {
        if (await LoadAsync(pathwayId, ct) is not { } tree)
        {
            return NotFound(pathwayId);
        }

        if (tree.Copy is not { } copy)
        {
            return Error.Conflict(tree.Live.Status == RuleStatus.Draft
                ? "This pathway has never been published; it is the draft. Edit it or reload the seed."
                : "There are no unpublished changes.");
        }

        Audit(PathwaysTable, copy.Id, tree.Live.Id, AuditAction.Delete, TreeSnapshot(copy), null, actor);
        db.Pathways.Remove(copy); // children cascade
        await db.SaveChangesAsync(ct);
        return ToResponse(await LoadAsync(pathwayId, ct) ?? tree);
    }

    // ---------------------------------------------------------------- publish

    public async Task<Result<PublishResponse>> PublishAsync(Guid pathwayId, string actor, CancellationToken ct)
    {
        if (await LoadAsync(pathwayId, ct) is not { } tree)
        {
            return NotFound(pathwayId);
        }

        var live = tree.Live;
        var working = tree.Working;
        if (tree.Copy is null && live.Status == RuleStatus.Published)
        {
            return Error.Conflict("There are no unpublished changes.");
        }

        var blockers = PublishGuard.Check(working);
        if (blockers.Count > 0)
        {
            return Error.Conflict(
                $"{blockers.Count} row(s) still need verifying ({RuleValues.TodoVerify} or no verified_at).",
                Blockers(blockers));
        }

        var before = live.Status == RuleStatus.Published ? TreeSnapshot(live) : null;
        var version = (await db.Pathways.Where(p => p.Status == RuleStatus.Published).MaxAsync(p => (int?)p.RuleVersion, ct) ?? 0) + 1;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (tree.Copy is { } copy)
        {
            CopyPathwayFields(copy, live);
            db.Requirements.RemoveRange(live.Requirements);
            db.PointsFactors.RemoveRange(live.PointsFactors);
            db.OccupationLists.RemoveRange(live.Occupations);
            await db.SaveChangesAsync(ct); // free (pathway_id, code) before the draft rows move in

            foreach (var row in Children(copy))
            {
                MoveToLive(row, live);
                row.Status = RuleStatus.Published;
                row.UpdatedAt = Now;
            }

            await db.SaveChangesAsync(ct);
            db.Pathways.Remove(copy);
        }
        else
        {
            foreach (var row in Children(live))
            {
                row.Status = RuleStatus.Published;
                row.UpdatedAt = Now;
            }
        }

        live.Status = RuleStatus.Published;
        live.RuleVersion = version;
        live.UpdatedAt = Now;
        await db.SaveChangesAsync(ct);

        // The guard checks verification; the snapshot mapper is what serves rules, so prove it accepts the result.
        db.ChangeTracker.Clear();
        var published = await Trees().AsNoTracking().Include(p => p.NextPathway).SingleAsync(p => p.Id == live.Id, ct);
        try
        {
            RuleSnapshotMapper.Map(await db.Countries.AsNoTracking().ToListAsync(ct), [published], []);
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(ct);
            return Error.Conflict(ex.Message);
        }

        Audit(PathwaysTable, live.Id, live.Id, AuditAction.Publish, before, TreeSnapshot(published), actor);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        rules.Invalidate();
        logger.LogInformation("Published {Slug} as rule version {Version}", live.Slug, version);
        return new PublishResponse(live.Id, live.Slug, version);
    }

    public async Task<LanguageMappingsStatusResponse> PublishLanguageMappingsAsync(string actor, CancellationToken ct)
    {
        var mappings = await db.LanguageScoreMappings.ToListAsync(ct);
        var ready = mappings.Where(m => m.Status == RuleStatus.Draft && PublishGuard.Check(m).Count == 0).ToList();
        foreach (var m in ready)
        {
            m.Status = RuleStatus.Published;
            m.UpdatedAt = Now;
            Audit(LanguageMappingsTable, m.Id, null, AuditAction.Publish, null, MappingLabel(m), actor);
        }

        await db.SaveChangesAsync(ct);
        if (ready.Count > 0)
        {
            rules.Invalidate();
            logger.LogInformation("Published {Count} language mapping(s)", ready.Count);
        }

        return MappingStatus(mappings);
    }

    // ---------------------------------------------------------------- draft copy plumbing

    private sealed record Tree(Pathway Live, Pathway? Copy)
    {
        public Pathway Working => Copy ?? Live;
    }

    private IQueryable<Pathway> Trees() => db.Pathways
        .Include(p => p.Country)
        .Include(p => p.Requirements)
        .Include(p => p.PointsFactors)
        .Include(p => p.Occupations)
        .AsSplitQuery();

    /// <summary>The live pathway for <paramref name="id"/> (live or draft-copy id) and its draft copy, tracked.</summary>
    private async Task<Tree?> LoadAsync(Guid id, CancellationToken ct)
    {
        var row = await db.Pathways.AsNoTracking().Where(p => p.Id == id).Select(p => new { p.Id, p.ReplacesId }).SingleOrDefaultAsync(ct);
        if (row is null)
        {
            return null;
        }

        var liveId = row.ReplacesId ?? row.Id;
        var both = await Trees().Where(p => p.Id == liveId || p.ReplacesId == liveId).ToListAsync(ct);
        return new Tree(both.Single(p => p.Id == liveId), both.SingleOrDefault(p => p.ReplacesId == liveId));
    }

    /// <summary>The tree's editable pathway, creating the draft copy of a published pathway on first write.</summary>
    private Pathway EnsureWorking(ref Tree tree)
    {
        if (tree.Copy is not null || tree.Live.Status != RuleStatus.Published)
        {
            return tree.Working;
        }

        var live = tree.Live;
        var copy = new Pathway
        {
            Id = Guid.NewGuid(),
            ReplacesId = live.Id,
            Status = RuleStatus.Draft,
            CountryId = live.CountryId,
            Country = live.Country,
            Slug = live.Slug,
            Name = live.Name,
            Summary = live.Summary,
            SourceUrl = live.SourceUrl,
            RuleVersion = live.RuleVersion,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        CopyPathwayFields(live, copy);
        copy.Requirements = live.Requirements.Select(r => new Requirement
        {
            Id = Guid.NewGuid(), ReplacesId = r.Id, Status = RuleStatus.Draft, CreatedAt = Now, UpdatedAt = Now,
            Field = r.Field, Operator = r.Operator, Value = r.Value, Unit = r.Unit, IsMandatory = r.IsMandatory,
            Fixability = r.Fixability, FixHint = r.FixHint, FixMonths = r.FixMonths, Label = r.Label,
            SourceUrl = r.SourceUrl, VerifiedAt = r.VerifiedAt, UnverifiedFields = [.. r.UnverifiedFields],
        }).ToList();
        copy.PointsFactors = live.PointsFactors.Select(f => new PointsFactor
        {
            Id = Guid.NewGuid(), ReplacesId = f.Id, Status = RuleStatus.Draft, CreatedAt = Now, UpdatedAt = Now,
            Factor = f.Factor, Variant = f.Variant, Min = f.Min, Max = f.Max, MatchValue = f.MatchValue, Points = f.Points,
            MaxPointsForFactor = f.MaxPointsForFactor,
            SourceUrl = f.SourceUrl, VerifiedAt = f.VerifiedAt, UnverifiedFields = [.. f.UnverifiedFields],
        }).ToList();
        copy.Occupations = live.Occupations.Select(o => new OccupationListEntry
        {
            Id = Guid.NewGuid(), ReplacesId = o.Id, Status = RuleStatus.Draft, CreatedAt = Now, UpdatedAt = Now,
            Code = o.Code, Title = o.Title, IsEligible = o.IsEligible,
            SourceUrl = o.SourceUrl, VerifiedAt = o.VerifiedAt, UnverifiedFields = [.. o.UnverifiedFields],
        }).ToList();
        db.Pathways.Add(copy);
        tree = tree with { Copy = copy };
        return copy;
    }

    private async Task<Result<AdminPathwayResponse>> WriteAsync(
        Guid pathwayId, string actor, CancellationToken ct, Func<Tree, Pathway, Task<Error?>> change)
    {
        if (await LoadAsync(pathwayId, ct) is not { } tree)
        {
            return NotFound(pathwayId);
        }

        var working = EnsureWorking(ref tree);
        if (await change(tree, working) is { } error)
        {
            return error;
        }

        Touch(working);
        await db.SaveChangesAsync(ct);
        return ToResponse(tree);
    }

    /// <summary>
    /// Resolves a child id to the row to edit: itself when it is already a draft row, else its counterpart in the
    /// pathway's draft copy (created on demand). An id of a published row whose draft counterpart was deleted is 404.
    /// </summary>
    private async Task<Result<AdminPathwayResponse>> WriteChildAsync<T>(
        Guid id, DbSet<T> set, Func<Pathway, List<T>> children, string actor, CancellationToken ct, Action<Tree, T> change)
        where T : SourcedRuleEntity
    {
        var pathwayId = await set.AsNoTracking().Where(r => r.Id == id)
            .Select(r => (Guid?)EF.Property<Guid>(r, nameof(Requirement.PathwayId))).SingleOrDefaultAsync(ct);
        if (pathwayId is null || await LoadAsync(pathwayId.Value, ct) is not { } tree)
        {
            return Error.NotFound($"No rule row {id}.");
        }

        var working = EnsureWorking(ref tree);
        var row = children(working).SingleOrDefault(r => r.Id == id || ReplacesOf(r) == id);
        if (row is null)
        {
            return Error.NotFound($"Row {id} was removed in the pathway's draft.");
        }

        change(tree, row);
        if (db.Entry(row).State == EntityState.Deleted)
        {
            children(working).Remove(row);
        }

        Touch(working);
        await db.SaveChangesAsync(ct);
        return ToResponse(tree);
    }

    private static Guid? ReplacesOf(SourcedRuleEntity row) => row switch
    {
        Requirement r => r.ReplacesId,
        PointsFactor f => f.ReplacesId,
        OccupationListEntry o => o.ReplacesId,
        _ => null,
    };

    /// <summary>Re-parents a draft-copy child onto the live pathway; it no longer replaces anything.</summary>
    private static void MoveToLive(SourcedRuleEntity row, Pathway live)
    {
        switch (row)
        {
            case Requirement r: (r.Pathway, r.PathwayId, r.ReplacesId) = (live, live.Id, null); break;
            case PointsFactor f: (f.Pathway, f.PathwayId, f.ReplacesId) = (live, live.Id, null); break;
            case OccupationListEntry o: (o.Pathway, o.PathwayId, o.ReplacesId) = (live, live.Id, null); break;
        }
    }

    private static IEnumerable<SourcedRuleEntity> Children(Pathway p) =>
        p.Requirements.Cast<SourcedRuleEntity>().Concat(p.PointsFactors).Concat(p.Occupations).ToList();

    private static IEnumerable<SourcedRuleEntity> Rows(Pathway p) => Children(p).Prepend(p);

    private static void CopyPathwayFields(Pathway from, Pathway to)
    {
        to.Name = from.Name;
        to.Type = from.Type;
        to.Audience = [.. from.Audience];
        to.IsPointsBased = from.IsPointsBased;
        to.PassMark = from.PassMark;
        to.Cutoff = from.Cutoff;
        to.CutoffAsOf = from.CutoffAsOf;
        to.NextPathwayId = from.NextPathwayId;
        to.TypicalDurationMonths = from.TypicalDurationMonths;
        to.Summary = from.Summary;
        to.SourceUrl = from.SourceUrl;
        to.VerifiedAt = from.VerifiedAt;
        to.UnverifiedFields = [.. from.UnverifiedFields];
    }

    // ---------------------------------------------------------------- request → entity

    private void Apply(Requirement row, RequirementRequest r)
    {
        row.Field = r.Field.Trim();
        row.Operator = Parse<RequirementOperator>(r.Operator);
        row.Value = r.Value is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) } v ? v.GetRawText() : null;
        row.Unit = string.IsNullOrWhiteSpace(r.Unit) ? null : r.Unit.Trim();
        row.IsMandatory = r.IsMandatory;
        row.Fixability = Parse<Fixability>(r.Fixability);
        row.FixHint = r.FixHint.Trim();
        row.FixMonths = r.FixMonths;
        row.Label = r.Label.Trim();
        SetSource(row, r.SourceUrl, r.VerifiedAt, r.UnverifiedFields);
    }

    private void Apply(PointsFactor row, PointsFactorRequest r)
    {
        row.Factor = r.Factor.Trim();
        row.Variant = Parse<PointsVariant>(r.Variant);
        row.Min = r.Min;
        row.Max = r.Max;
        row.MatchValue = string.IsNullOrWhiteSpace(r.MatchValue) ? null : r.MatchValue.Trim();
        row.Points = r.Points;
        row.MaxPointsForFactor = r.MaxPointsForFactor;
        SetSource(row, r.SourceUrl, r.VerifiedAt, r.UnverifiedFields);
    }

    private void Apply(OccupationListEntry row, OccupationRow r, string pathwaySource)
    {
        row.Title = r.Title;
        row.IsEligible = r.IsEligible;
        row.SourceUrl = r.SourceUrl ?? pathwaySource;
        row.VerifiedAt = r.VerifiedAt;
        row.UnverifiedFields = [];
        row.UpdatedAt = Now;
    }

    private void SetSource(SourcedRuleEntity row, string sourceUrl, DateOnly? verifiedAt, IReadOnlyList<string> unverified)
    {
        row.SourceUrl = sourceUrl.Trim();
        row.VerifiedAt = verifiedAt;
        row.UnverifiedFields = unverified.Distinct().ToList();
        row.UpdatedAt = Now;
    }

    private void Touch(Pathway working) => working.UpdatedAt = Now;

    private DateTimeOffset Now => clock.GetUtcNow();

    private static TEnum Parse<TEnum>(string text)
        where TEnum : struct, Enum =>
        EnumText.TryParse<TEnum>(text, out var value) ? value : throw new ArgumentException($"Unvalidated {typeof(TEnum).Name}: {text}");

    // ---------------------------------------------------------------- audit

    private void Audit(string entity, Guid entityId, Guid? pathwayId, AuditAction action, object? oldValue, object? newValue, string actor) =>
        db.RuleAuditLog.Add(new RuleAuditLogEntry
        {
            Id = Guid.NewGuid(),
            Entity = entity,
            EntityId = entityId,
            PathwayId = pathwayId,
            Action = action,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue, Json),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue, Json),
            Actor = actor,
            At = Now,
        });

    private static object PathwayFields(Pathway p) => new
    {
        p.Name, Type = EnumText.Of(p.Type), Audience = p.Audience.Select(EnumText.Of).ToList(), p.IsPointsBased, p.PassMark,
        p.Cutoff, p.CutoffAsOf, p.NextPathwayId, p.TypicalDurationMonths, p.Summary, p.SourceUrl, p.VerifiedAt,
        UnverifiedFields = p.UnverifiedFields.ToList(),
    };

    private object TreeSnapshot(Pathway p) => new
    {
        p.Slug, p.RuleVersion, Pathway = PathwayFields(p),
        Requirements = p.Requirements.Select(ToResponse).ToList(),
        PointsFactors = p.PointsFactors.Select(ToResponse).ToList(),
        Occupations = p.Occupations.Select(ToResponse).ToList(),
    };

    // ---------------------------------------------------------------- entity → response

    private AdminPathwayResponse ToResponse(Tree tree)
    {
        var live = tree.Live;
        var w = tree.Working;
        return new AdminPathwayResponse(
            live.Id, live.Slug, live.Country!.Code, EnumText.Of(live.Status), tree.Copy is not null, live.RuleVersion,
            w.Name, EnumText.Of(w.Type), w.Audience.Select(EnumText.Of).ToList(), w.IsPointsBased, w.PassMark, w.Cutoff,
            w.CutoffAsOf, w.NextPathwayId, w.TypicalDurationMonths, w.Summary, w.SourceUrl, w.VerifiedAt, w.UnverifiedFields,
            IsStale(w),
            w.Requirements.OrderBy(r => r.Label, StringComparer.Ordinal).Select(ToResponse).ToList(),
            w.PointsFactors.OrderBy(f => f.Factor, StringComparer.Ordinal).ThenBy(f => f.Variant).ThenBy(f => f.Min).ThenBy(f => f.MatchValue)
                .Select(ToResponse).ToList(),
            w.Occupations.OrderBy(o => o.Code, StringComparer.Ordinal).Select(ToResponse).ToList(),
            PublishGuard.Check(w).Select(b => new PublishBlockerResponse(b.Entity, b.Label, b.Reason)).ToList());
    }

    private AdminRequirementResponse ToResponse(Requirement r) => new(
        r.Id, EnumText.Of(r.Status), r.Field, EnumText.Of(r.Operator), ParseJson(r.Value), r.Unit, r.IsMandatory,
        EnumText.Of(r.Fixability), r.FixHint, r.FixMonths, r.Label, r.SourceUrl, r.VerifiedAt, r.UnverifiedFields, IsStale(r));

    private AdminPointsFactorResponse ToResponse(PointsFactor f) => new(
        f.Id, EnumText.Of(f.Status), f.Factor, EnumText.Of(f.Variant), f.Min, f.Max, f.MatchValue, f.Points, f.MaxPointsForFactor,
        f.SourceUrl, f.VerifiedAt, f.UnverifiedFields, IsStale(f));

    private AdminOccupationResponse ToResponse(OccupationListEntry o) => new(
        o.Id, EnumText.Of(o.Status), o.Code, o.Title, o.IsEligible, o.SourceUrl, o.VerifiedAt, o.UnverifiedFields, IsStale(o));

    private bool IsStale(SourcedRuleEntity row) => row.VerifiedAt is { } at && at < StaleBefore;

    private static JsonElement? ParseJson(string? json) => json is null ? null : JsonDocument.Parse(json).RootElement.Clone();

    private static LanguageMappingsStatusResponse MappingStatus(IReadOnlyCollection<LanguageScoreMapping> mappings) => new(
        mappings.Count(m => m.Status == RuleStatus.Published),
        mappings.Count(m => m.Status == RuleStatus.Draft && PublishGuard.Check(m).Count == 0),
        mappings.Count(m => m.Status == RuleStatus.Draft && PublishGuard.Check(m).Count > 0));

    private static string FactorLabel(PointsFactor f) =>
        $"{f.Factor} ({EnumText.Of(f.Variant)}) {f.MatchValue ?? $"{f.Min?.ToString() ?? "…"}–{f.Max?.ToString() ?? "…"}"}";

    private static string MappingLabel(LanguageScoreMapping m) =>
        $"{m.Test}{(m.TestVariant is null ? "" : $" ({m.TestVariant})")} {m.Skill} ≥ {m.MinScore} → {m.TargetScale} {m.TargetLevel}";

    private static Dictionary<string, string[]> Blockers(IEnumerable<PublishBlocker> blockers) =>
        blockers.GroupBy(b => $"{b.Entity}: {b.Label}").ToDictionary(g => g.Key, g => g.Select(b => b.Reason).ToArray());

    private static Error NotFound(Guid id) => Error.NotFound($"No pathway {id}.");

    private static Error Invalid(string field, string message) =>
        Error.Validation(new Dictionary<string, string[]> { [field] = [message] });
}
