using System.Text.Json;
using LvInfrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LvApi.Extensions;

public static class HealthCheckExtensions
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database", tags: [ReadyTag]);
        return services;
    }

    /// <summary>
    /// /health: liveness, the process answers (no dependencies, used by Render's health check so
    /// a slow database never gets a healthy instance restarted).
    /// /health/ready: readiness, the database is reachable. Both are anonymous.
    /// </summary>
    public static WebApplication MapAppHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(
                "/health",
                new HealthCheckOptions { Predicate = _ => false, ResponseWriter = WriteJsonAsync }
            )
            .AllowAnonymous();

        app.MapHealthChecks(
                "/health/ready",
                new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains(ReadyTag),
                    ResponseWriter = WriteJsonAsync,
                }
            )
            .AllowAnonymous();

        return app;
    }

    private static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                error = e.Value.Exception?.Message,
            }),
        };
        return JsonSerializer.SerializeAsync(context.Response.Body, payload);
    }
}
