using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LvApi;
using LvApi.Configuration;
using LvApi.Extensions;
using LvApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
builder.UsePortFromEnvironment();

builder.Host.UseSerilog(
    (context, services, configuration) =>
    {
        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);

        // Containers log to stdout (collected by the platform); their disk is ephemeral and the
        // non-root app user cannot write next to the binaries. Rolling files are a dev aid only.
        if (!context.HostingEnvironment.IsDevelopment())
            return;

        configuration
            .WriteTo.File(
                Path.Combine(context.HostingEnvironment.ContentRootPath, "Logs", "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                formatProvider: CultureInfo.InvariantCulture
            )
            .WriteTo.File(
                Path.Combine(context.HostingEnvironment.ContentRootPath, "Logs", "errors-.log"),
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90,
                formatProvider: CultureInfo.InvariantCulture
            );
    }
);

// TODO: confirmar que la empresa califica para la licencia Community de QuestPDF (umbral de ingresos anuales) antes de producción.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Add services to the container.
builder
    .Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Malformed bodies (wrong JSON types, unparsable dates...) get the same Spanish error shape as
// the rest of the API instead of the default English ProblemDetails.
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(
        new
        {
            statusCode = StatusCodes.Status400BadRequest,
            code = "validation",
            message = InvalidRequestMessage.For(context.ModelState.Keys),
        }
    )
);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "LvBuild API",
            Version = "v1",
            Description = ApiDocumentation.Description,
        }
    );

    // XML comments of the controllers (GenerateDocumentationFile in LvApi.csproj); controller
    // summaries become the tag descriptions.
    options.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml"),
        includeControllerXmlComments: true
    );

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                "Paste only the accessToken from POST /api/auth/login (Swagger adds 'Bearer').",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        }
    );

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );

    options.CustomSchemaIds(type => GetSchemaId(type));
});

static string GetSchemaId(Type type)
{
    if (!type.IsGenericType)
        return (type.FullName ?? type.Name).Replace("+", ".");

    var genericTypeName = type.Name.Split('`')[0];
    var genericArgNames = type.GetGenericArguments().Select(t => GetSchemaId(t).Split('.').Last());
    return $"{type.Namespace}.{genericTypeName}Of{string.Join("And", genericArgNames)}";
}

builder.Services.AddAppOptions(builder.Configuration);
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();

// Configured from the validated JwtOptions instead of reading raw configuration up front, so a
// missing key is reported by options validation at startup with an actionable message.
builder
    .Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>(
        (options, jwt) =>
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Value.Issuer,
                ValidAudience = jwt.Value.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Key)),
            }
    );

builder.Services.AddAuthorization();

builder.Services.AddCors();
builder
    .Services.AddOptions<CorsOptions>()
    .Configure<IOptions<CorsOriginsOptions>>(
        (options, origins) =>
            options.AddPolicy(
                CorsPolicies.Default,
                policy =>
                    policy
                        .WithOrigins(origins.Value.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
            )
    );

builder.Services.AddReverseProxySupport();
builder.Services.AddAppHealthChecks();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        RateLimiterPolicies.Login,
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }
            )
    );

    options.AddPolicy(
        RateLimiterPolicies.ForgotPassword,
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }
            )
    );
});

var app = builder.Build();

// First, so request logging and the per-IP login rate limiter see the real client address.
app.UseTransportSecurity();

app.UseSerilogRequestLogging();

// Swagger: always in Development; elsewhere only with Swagger:Enabled=true (the public demo).
if (
    app.Environment.IsDevelopment()
    || app.Services.GetRequiredService<IOptions<ApiDocsOptions>>().Value.Enabled
)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "LvBuild API v1");
        options.DocumentTitle = "LvBuild API";
    });
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(CorsPolicies.Default);

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();
app.MapAppHealthChecks();

await app.InitializeDatabaseAsync();

await app.RunAsync();

public partial class Program { }
