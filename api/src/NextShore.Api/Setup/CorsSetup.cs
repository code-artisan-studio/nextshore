namespace NextShore.Api.Setup;

public static class CorsSetup
{
    public const string PolicyName = "web";

    /// <summary>
    /// Allowed origins come from <c>Cors:Origins</c> (comma-separated) only. The dev origin lives in
    /// appsettings.Development.json, so any other environment allows none until <c>Cors__Origins</c> is set.
    /// </summary>
    public static IServiceCollection AddWebCors(this IServiceCollection services, IConfiguration config) =>
        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(Origins(config))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Retry-After")));

    /// <summary>
    /// A missing <c>Cors__Origins</c> only shows up as blocked requests in the browser; say so in the logs at startup.
    /// The API itself still works, so this warns rather than refusing to start.
    /// </summary>
    public static void WarnIfNoOrigins(this WebApplication app)
    {
        if (Origins(app.Configuration).Length == 0)
        {
            app.Logger.LogWarning("No CORS origins configured (Cors__Origins): browsers will block the web app's requests.");
        }
    }

    private static string[] Origins(IConfiguration config) =>
        (config["Cors:Origins"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
