using Microsoft.AspNetCore.HttpOverrides;
using NextShore.Api.Endpoints;
using NextShore.Api.Setup;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHealthChecks(); // DB check added with the DbContext (T06).
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

app.Run();
