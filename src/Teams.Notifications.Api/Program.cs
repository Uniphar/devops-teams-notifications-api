using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using Teams.Notifications.Api;
using Teams.Notifications.Api.Agents;
using Teams.Notifications.Api.DelegatingHandlers;
using Teams.Notifications.Api.Middlewares;
using Teams.Notifications.Api.Services;
using IMiddleware = Microsoft.Agents.Builder.IMiddleware;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

const string appPathPrefix = "devops-teams-notification-api";

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment.EnvironmentName ?? throw new NoNullAllowedException("ASPNETCORE_ENVIRONMENT environment variable has to be set.");

TokenCredential credentials = new DefaultAzureCredential();

// this is what the bot is communicating on
builder.Services.AddHttpClient(typeof(RestChannelServiceClientFactory).FullName!).AddHttpMessageHandler<RequestAndResponseLoggerHandler>();
// Register Semantic Kernel
builder.Services.AddKernel();

// Values from app registration
var clientId = builder.Configuration["AZURE_CLIENT_ID"] ?? throw new NoNullAllowedException("ClientId is required");
var tenantId = builder.Configuration["AZURE_TENANT_ID"] ?? throw new NoNullAllowedException("TenantId is required");

var clientSecret = builder.Configuration["ClientSecret"];

// will use workload if available
if (!string.IsNullOrWhiteSpace(clientSecret)) credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
builder.Services.AddSingleton(new GraphServiceClient(credentials));
builder.Services.AddTransient<RequestAndResponseLoggerHandler>();
builder.Services.AddTransient<ICardManagerService, CardManagerService>();
builder.Services.AddTransient<ITeamsManagerService, TeamsManagerService>();
builder.Services.AddHealthChecks();
builder.Services.AddAgentAspNetAuthentication(builder.Configuration);
builder.Services.AddMemoryCache();
// Add ApplicationOptions
builder.AddAgentApplicationOptions();
builder.AddAgent<CardActionAgent>();
builder
    .Services
    .AddControllers(o =>
    {
        o.Filters.Add<ExceptionHandlingFilter>();
        o.Conventions.Add(new HideChannelApi());
        o.Conventions.Add(new GlobalRouteConvention(appPathPrefix));
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
if (environment != "local")
{
// key vault is required for ApplicationInsights, since it needs the connection string
    builder.Configuration.AddAzureKeyVault(new Uri($"https://uni-devops-app-{environment}-kv.vault.azure.net/"), credentials);
    builder.Services.AddApplicationInsightsTelemetry(options => options.EnableAdaptiveSampling = false);
    builder.Services.AddApplicationInsightsTelemetryWorkerService(options => options.EnableAdaptiveSampling = false);
}

builder.Services.AddSingleton<IMiddleware[]>(sp => [new CaptureMiddleware()]);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Version = "v1",
            Title = "Teams notifications platform",
            Description = "Teams notifications platform"
        });
    c.IncludeXmlComments(Assembly.GetExecutingAssembly());
    c.EnableAnnotations();
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme,
        new OpenApiSecurityScheme
        {
            Name = "Basic Authentication",
            Description = "Enter test username and password",
            In = ParameterLocation.Header,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT",
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                ClientCredentials = new OpenApiOAuthFlow()
            }
        });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                },
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                Name = JwtBearerDefaults.AuthenticationScheme,
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// Register IStorage.  For development, MemoryStorage is suitable.
// For production Agents, persisted storage should be used so
// that state survives Agent restarts, and operate correctly
// in a cluster of Agent instances.
builder.Services.AddSingleton<IStorage, MemoryStorage>();


var app = builder.Build();
app.MapHealthChecks("/health");
app.UseSwagger(options => { options.RouteTemplate = $"{appPathPrefix}/swagger/{{documentname}}/swagger.json"; });
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "V1");
    c.RoutePrefix = $"{appPathPrefix}/swagger";
});
app.MapControllers();

app
    .MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) => { await adapter.ProcessAsync(request, response, agent, cancellationToken); })
    .RequireAuthorization()
    // exclude from api explorer
    .ExcludeFromDescription();
app
    .MapGet("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) => { await adapter.ProcessAsync(request, response, agent, cancellationToken); })
    .RequireAuthorization()
    // exclude from api explorer
    .ExcludeFromDescription();

app.Run();