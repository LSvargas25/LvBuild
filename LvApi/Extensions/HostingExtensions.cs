using System.Globalization;
using LvApi.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace LvApi.Extensions;

public static class HostingExtensions
{
    /// <summary>
    /// Render (and most PaaS) tell the container which port to listen on through PORT. When it
    /// is not set, the aspnet image default applies (ASPNETCORE_HTTP_PORTS=8080).
    /// </summary>
    public static WebApplicationBuilder UsePortFromEnvironment(this WebApplicationBuilder builder)
    {
        var port = Environment.GetEnvironmentVariable("PORT");
        if (string.IsNullOrWhiteSpace(port))
            return builder;

        if (!int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            throw new InvalidOperationException($"PORT must be a number, got '{port}'.");

        builder.WebHost.UseUrls($"http://+:{number}");
        return builder;
    }

    /// <summary>
    /// Behind Render's TLS-terminating proxy the app only sees plain HTTP from the proxy, so the
    /// real client IP (used by the login rate limiter) and scheme come from X-Forwarded-*.
    /// Render does not publish its proxy addresses, hence the cleared known networks/proxies;
    /// this is only enabled when ReverseProxy:Enabled=true.
    /// </summary>
    public static IServiceCollection AddReverseProxySupport(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
        return services;
    }

    /// <summary>
    /// Behind the proxy: honor X-Forwarded-* (must run first). Otherwise: redirect HTTP to HTTPS.
    /// The proxy already forces HTTPS, and redirecting there would loop on the plain-HTTP hop.
    /// </summary>
    public static WebApplication UseTransportSecurity(this WebApplication app)
    {
        var proxy = app.Services.GetRequiredService<IOptions<ReverseProxyOptions>>().Value;
        if (proxy.Enabled)
            app.UseForwardedHeaders();
        else
            app.UseHttpsRedirection();
        return app;
    }
}
