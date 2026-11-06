using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextShore.Api.IntegrationTests.Api;
using NextShore.Api.IntegrationTests.Database;
using NextShore.Application.Admin;
using NextShore.Application.Countries;
using NextShore.Application.Results;
using NextShore.Domain.Enums;

namespace NextShore.Api.IntegrationTests.Admin;

/// <summary>Admin rule editing on a real Postgres with the seed loaded (all drafts). ca-pgwp is fully verified.</summary>
[Collection(PostgresCollection.Name)]
public sealed class AdminApiTests(PostgresFixture postgres)
{
    private const string Pgwp = "ca-pgwp";

    private sealed record Context(AdminFactory Factory, HttpClient Admin, HttpClient Public, string ConnectionString) : IDisposable
    {
        public void Dispose() => Factory.Dispose();
    }

    private async Task<Context> StartAsync()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await DatabaseHelpers.SeedAsync(connectionString);
        var factory = new AdminFactory { DatabaseConnectionString = connectionString };
        return new Context(factory, factory.CreateAdminClient(), factory.CreateClient(), connectionString);
    }

    private static async Task<AdminPathwayListItem> FindAsync(HttpClient admin, string slug) =>
        (await admin.GetFromJsonAsync<List<AdminPathwayListItem>>("/api/v1/admin/pathways"))!.Single(p => p.Slug == slug);

    private static async Task<AdminPathwayResponse> GetAsync(HttpClient admin, Guid id) =>
        (await admin.GetFromJsonAsync<AdminPathwayResponse>($"/api/v1/admin/pathways/{id}"))!;

    private static async Task<T> OkAsync<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task PublishAsync(HttpClient admin, Guid id)
    {
        await OkAsync<LanguageMappingsStatusResponse>(await admin.PostAsync("/api/v1/admin/language-mappings/publish", null));
        await OkAsync<PublishResponse>(await admin.PostAsync($"/api/v1/admin/pathways/{id}/publish", null));
    }

    private static RequirementRequest Edit(AdminRequirementResponse r, string? label = null) => new(
        r.Field, r.Operator, r.Value, r.Unit, r.IsMandatory, r.Fixability, r.FixHint, r.FixMonths, label ?? r.Label,
        r.SourceUrl, r.VerifiedAt, r.UnverifiedFields);

    private static async Task<List<string>> LiveRequirementLabelsAsync(HttpClient client)
    {
        var evaluation = await OkAsync<EvaluationResponse>(await client.PostAsJsonAsync("/api/v1/evaluations", Personas.Student));
        return evaluation.Results.Where(r => r.PathwaySlug == Pgwp).SelectMany(r => r.Requirements).Select(r => r.Label).ToList();
    }

    [Fact]
    public async Task Lists_every_seeded_pathway_as_draft_with_its_blockers()
    {
        using var ctx = await StartAsync();

        var list = await ctx.Admin.GetFromJsonAsync<List<AdminPathwayListItem>>("/api/v1/admin/pathways");

        Assert.Equal(10, list!.Count);
        Assert.All(list, p => Assert.Equal(("draft", false, 0), (p.Status, p.HasDraft, p.RuleVersion)));
        Assert.Equal(0, list.Single(p => p.Slug == Pgwp).PublishBlockers);
        Assert.Equal(1, list.Single(p => p.Slug == "ca-study-permit").PublishBlockers);
    }

    [Fact]
    public async Task Publish_is_409_with_the_blockers_while_a_row_is_unverified()
    {
        using var ctx = await StartAsync();
        var studyPermit = await FindAsync(ctx.Admin, "ca-study-permit");

        var response = await ctx.Admin.PostAsync($"/api/v1/admin/pathways/{studyPermit.Id}/publish", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(problem.GetProperty("errors").EnumerateObject());
        Assert.Equal("draft", (await FindAsync(ctx.Admin, "ca-study-permit")).Status);
    }

    [Fact]
    public async Task First_publish_makes_the_pathway_live_and_audits_it()
    {
        using var ctx = await StartAsync();
        var pgwp = await FindAsync(ctx.Admin, Pgwp);

        await PublishAsync(ctx.Admin, pgwp.Id);

        var countries = await ctx.Public.GetFromJsonAsync<List<CountryResponse>>("/api/v1/countries");
        Assert.Equal([Pgwp], countries!.SelectMany(c => c.Pathways).Select(p => p.Slug));
        var listed = await FindAsync(ctx.Admin, Pgwp);
        Assert.Equal(("published", false, 1), (listed.Status, listed.HasDraft, listed.RuleVersion));
        var audit = await ctx.Admin.GetFromJsonAsync<AuditPageResponse>($"/api/v1/admin/audit?pathwayId={pgwp.Id}");
        var entry = Assert.Single(audit!.Items);
        Assert.Equal(("publish", "user_admin", pgwp.Id), (entry.Action, entry.Actor, entry.EntityId));
        Assert.Null(entry.OldValue);
    }

    [Fact]
    public async Task Edits_to_a_published_pathway_stay_in_a_draft_until_publish()
    {
        using var ctx = await StartAsync();
        var id = (await FindAsync(ctx.Admin, Pgwp)).Id;
        await PublishAsync(ctx.Admin, id);
        var before = await GetAsync(ctx.Admin, id);
        var liveLabels = await LiveRequirementLabelsAsync(ctx.Public);
        var target = before.Requirements[0];

        var edited = await OkAsync<AdminPathwayResponse>(
            await ctx.Admin.PutAsJsonAsync($"/api/v1/admin/requirements/{target.Id}", Edit(target, "Edited label")));

        Assert.True(edited.HasDraft);
        Assert.Equal(id, edited.Id);
        Assert.Contains(edited.Requirements, r => r.Label == "Edited label" && r.Status == "draft");
        Assert.Equal(liveLabels, await LiveRequirementLabelsAsync(ctx.Public)); // live rules untouched

        // The published id still resolves to the draft row, so a second edit through it doesn't fork another copy.
        var again = await OkAsync<AdminPathwayResponse>(
            await ctx.Admin.PutAsJsonAsync($"/api/v1/admin/requirements/{target.Id}", Edit(target, "Edited twice")));
        Assert.Equal(before.Requirements.Count, again.Requirements.Count);

        var published = await OkAsync<PublishResponse>(await ctx.Admin.PostAsync($"/api/v1/admin/pathways/{id}/publish", null));

        Assert.Equal((id, 2), (published.Id, published.RuleVersion));
        var liveAfter = await LiveRequirementLabelsAsync(ctx.Public);
        Assert.Contains("Edited twice", liveAfter);
        Assert.DoesNotContain(target.Label, liveAfter);
        var after = await GetAsync(ctx.Admin, id);
        Assert.False(after.HasDraft);
        Assert.All(after.Requirements, r => Assert.Equal("published", r.Status));

        await using var db = PostgresFixture.CreateContext(ctx.ConnectionString);
        Assert.Equal(1, await db.Pathways.CountAsync(p => p.Slug == Pgwp));
        Assert.Equal(before.Requirements.Count, await db.Requirements.CountAsync(r => r.PathwayId == id));
        Assert.False(await db.Requirements.AnyAsync(r => r.ReplacesId != null));
    }

    [Fact]
    public async Task Delete_and_discard_work_on_the_draft()
    {
        using var ctx = await StartAsync();
        var id = (await FindAsync(ctx.Admin, Pgwp)).Id;
        await PublishAsync(ctx.Admin, id);
        var before = await GetAsync(ctx.Admin, id);
        var target = before.Requirements[0];
        ctx.Factory.Clock.Advance(TimeSpan.FromMinutes(1));

        var afterDelete = await OkAsync<AdminPathwayResponse>(await ctx.Admin.DeleteAsync($"/api/v1/admin/requirements/{target.Id}"));
        Assert.Equal(before.Requirements.Count - 1, afterDelete.Requirements.Count);
        Assert.Equal(HttpStatusCode.NotFound, (await ctx.Admin.DeleteAsync($"/api/v1/admin/requirements/{target.Id}")).StatusCode);
        ctx.Factory.Clock.Advance(TimeSpan.FromMinutes(1));

        var discarded = await OkAsync<AdminPathwayResponse>(await ctx.Admin.DeleteAsync($"/api/v1/admin/pathways/{id}/draft"));

        Assert.False(discarded.HasDraft);
        Assert.Equal(before.Requirements.Select(r => r.Id), discarded.Requirements.Select(r => r.Id));
        Assert.Equal(HttpStatusCode.Conflict, (await ctx.Admin.PostAsync($"/api/v1/admin/pathways/{id}/publish", null)).StatusCode);
        var actions = (await ctx.Admin.GetFromJsonAsync<AuditPageResponse>($"/api/v1/admin/audit?pathwayId={id}"))!
            .Items.Select(a => (a.Entity, a.Action));
        Assert.Equal([("pathways", "delete"), ("requirements", "delete"), ("pathways", "publish")], actions);
    }

    [Fact]
    public async Task Verifying_the_last_unverified_value_unblocks_publish()
    {
        using var ctx = await StartAsync();
        var p = await GetAsync(ctx.Admin, (await FindAsync(ctx.Admin, "ca-study-permit")).Id);
        var blocker = Assert.Single(p.PublishBlockers);
        Assert.Equal(("pathway", "TODO_VERIFY: typical_duration_months"), (blocker.Entity, blocker.Reason)); // seed state

        var request = new PathwayUpdateRequest(p.Name, p.Type, p.Audience, p.IsPointsBased, p.PassMark, p.Cutoff, p.CutoffAsOf,
            p.NextPathwayId, TypicalDurationMonths: 24, p.Summary, p.SourceUrl, p.VerifiedAt, UnverifiedFields: []);
        var verified = await OkAsync<AdminPathwayResponse>(await ctx.Admin.PutAsJsonAsync($"/api/v1/admin/pathways/{p.Id}", request));

        Assert.Empty(verified.PublishBlockers);
        await PublishAsync(ctx.Admin, p.Id);
        Assert.Equal("published", (await FindAsync(ctx.Admin, "ca-study-permit")).Status);
    }

    [Fact]
    public async Task Added_rows_are_drafts_of_the_pathway()
    {
        using var ctx = await StartAsync();
        var id = (await FindAsync(ctx.Admin, Pgwp)).Id;
        await PublishAsync(ctx.Admin, id);
        var template = (await GetAsync(ctx.Admin, id)).Requirements[0];

        var added = await OkAsync<AdminPathwayResponse>(await ctx.Admin.PostAsJsonAsync(
            $"/api/v1/admin/pathways/{id}/requirements", Edit(template, "Brand new")));

        var row = added.Requirements.Single(r => r.Label == "Brand new");
        Assert.Equal("draft", row.Status);
        var audit = await ctx.Admin.GetFromJsonAsync<AuditPageResponse>($"/api/v1/admin/audit?pathwayId={id}&entity=requirements");
        Assert.Equal(("create", row.Id), (audit!.Items[0].Action, audit.Items[0].EntityId));
    }

    [Fact]
    public async Task Invalid_requirement_is_400_with_field_errors()
    {
        using var ctx = await StartAsync();
        var pgwp = await GetAsync(ctx.Admin, (await FindAsync(ctx.Admin, Pgwp)).Id);
        var bad = Edit(pgwp.Requirements[0]) with
        {
            Operator = "in",
            Value = JsonDocument.Parse("7").RootElement,
            SourceUrl = "not a url",
            UnverifiedFields = ["cutoff"],
        };

        var response = await ctx.Admin.PostAsJsonAsync($"/api/v1/admin/pathways/{pgwp.Id}/requirements", bad);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["sourceUrl", "unverifiedFields[0]", "value"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Occupation_csv_upserts_by_code_and_reports_bad_lines()
    {
        using var ctx = await StartAsync();
        var pgwp = await GetAsync(ctx.Admin, (await FindAsync(ctx.Admin, Pgwp)).Id);

        var bad = await ctx.Admin.PostAsync($"/api/v1/admin/pathways/{pgwp.Id}/occupations",
            new StringContent("code,title,is_eligible\n,No code,true\n99999,Bad flag,maybe\n", Encoding.UTF8, "text/csv"));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var problem = await bad.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["line 2", "line 3"], problem!.Errors.Keys.Order());

        const string csv = "code,title,is_eligible,verified_at\n99901,\"Testers, software\",true,2026-10-09\n99902,Other,no,\n";
        var first = await OkAsync<OccupationImportResponse>(await ctx.Admin.PostAsync(
            $"/api/v1/admin/pathways/{pgwp.Id}/occupations", new StringContent(csv, Encoding.UTF8, "text/csv")));
        var second = await OkAsync<OccupationImportResponse>(await ctx.Admin.PostAsync(
            $"/api/v1/admin/pathways/{pgwp.Id}/occupations", new StringContent(csv, Encoding.UTF8, "text/csv")));

        Assert.Equal((2, 0), (first.Created, first.Updated));
        Assert.Equal((0, 2), (second.Created, second.Updated));
        var testers = second.Pathway.Occupations.Single(o => o.Code == "99901");
        Assert.Equal(("Testers, software", true, pgwp.SourceUrl), (testers.Title, testers.IsEligible, testers.SourceUrl));
        Assert.Contains(second.Pathway.PublishBlockers, b => b.Label == "99902"); // no verified_at
    }

    [Fact]
    public async Task Stale_lists_published_rows_past_the_threshold()
    {
        using var ctx = await StartAsync();
        var id = (await FindAsync(ctx.Admin, Pgwp)).Id;
        await PublishAsync(ctx.Admin, id);

        var fresh = await ctx.Admin.GetFromJsonAsync<List<StaleRuleResponse>>("/api/v1/admin/rules/stale");
        ctx.Factory.Clock.Advance(TimeSpan.FromDays(120));
        var later = await ctx.Admin.GetFromJsonAsync<List<StaleRuleResponse>>("/api/v1/admin/rules/stale?days=90");

        Assert.Empty(fresh!);
        Assert.Contains(later!, s => s.Entity == "pathways" && s.Id == id && s.DaysSinceVerified >= 120);
        Assert.Contains(later!, s => s.Entity == "language_score_mappings");
        Assert.True((await FindAsync(ctx.Admin, Pgwp)).StaleRules > 0);
    }

    [Fact]
    public async Task Seed_reload_leaves_published_pathways_and_their_drafts_alone()
    {
        using var ctx = await StartAsync();
        var id = (await FindAsync(ctx.Admin, Pgwp)).Id;
        await PublishAsync(ctx.Admin, id);
        var target = (await GetAsync(ctx.Admin, id)).Requirements[0];
        await OkAsync<AdminPathwayResponse>(await ctx.Admin.PutAsJsonAsync($"/api/v1/admin/requirements/{target.Id}", Edit(target, "Draft edit")));

        await DatabaseHelpers.SeedAsync(ctx.ConnectionString);

        var after = await GetAsync(ctx.Admin, id);
        Assert.True(after.HasDraft);
        Assert.Contains(after.Requirements, r => r.Label == "Draft edit");
        await using var db = PostgresFixture.CreateContext(ctx.ConnectionString);
        Assert.Equal(RuleStatus.Published, (await db.Pathways.SingleAsync(p => p.Id == id)).Status);
    }
}
