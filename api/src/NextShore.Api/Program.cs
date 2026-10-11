using Microsoft.AspNetCore.HttpOverrides;
using NextShore.Api.Endpoints;
using NextShore.Api.Setup;
using NextShore.Application;
using NextShore.Application.Engine;
using NextShore.Infrastructure;
using NextShore.Infrastructure.Seed;
using Serilog;

// `dotnet run --project src/NextShore.Api -- seed [dir]` loads api/seed/*.json as drafts, then exits.
var seedMode = args is ["seed", ..];

var builder = WebApplication.CreateBuilder(seedMode ? [] : args);

builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "NextShore API";
    document.Info.Version = "v1";
    return Task.CompletedTask;
}));
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddOptions<EngineSettings>()
    .Bind(builder.Configuration.GetSection(EngineSettings.Section))
    .Validate(s => s.ExchangeRates.Count > 0 && s.ExchangeRates.Values.All(r => r > 0),
        "Engine:ExchangeRates needs a positive rate (units per 1 USD) for each currency the rules use.")
    .ValidateOnStart();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebCors(builder.Configuration);
builder.Services.AddNextShoreRateLimiting();

// Railway terminates TLS at its proxy; trust the single hop it adds so rate limits key on the client IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (seedMode)
{
    var directory = Path.GetFullPath(args.ElementAtOrDefault(1)
        ?? Path.Combine(app.Environment.ContentRootPath, "..", "..", "seed"));
    await using var scope = app.Services.CreateAsyncScope();
    try
    {
        await scope.ServiceProvider.GetRequiredService<SeedLoader>().LoadAsync(directory);
    }
    catch (SeedException ex)
    {
        app.Logger.LogError("Seed failed, nothing was written: {Message}", ex.Message);
        Environment.ExitCode = 1;
    }

    return;
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();
app.UseCors(CorsSetup.PolicyName);
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api/v1");
api.MapHealthEndpoints();
api.MapRulesEndpoints();
api.MapEvaluationsEndpoints();

app.Run();
