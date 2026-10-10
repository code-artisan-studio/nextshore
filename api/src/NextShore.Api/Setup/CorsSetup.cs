namespace NextShore.Api.Setup;

public static class CorsSetup
{
    public const string PolicyName = "web";

    /// <summary>Allowed origins come from <c>Cors:Origins</c> (comma-separated) only.</summary>
    public static IServiceCollection AddWebCors(this IServiceCollection services, IConfiguration config)
    {
        var origins = (config["Cors:Origins"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Retry-After")));
    }
}
