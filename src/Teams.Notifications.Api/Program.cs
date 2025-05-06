using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Hosting;
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


// this is what the bot is communicating on
builder.Services.AddHttpClient(typeof(RestChannelServiceClientFactory).FullName!).AddHttpMessageHandler<RequestAndResponseLoggerHandler>();
// Register Semantic Kernel
builder.Services.AddKernel();

// Values from app registration
var clientId = builder.Configuration["AZURE_CLIENT_ID"] ?? throw new NoNullAllowedException("ClientId is required");
var tenantId = builder.Configuration["AZURE_TENANT_ID"] ?? throw new NoNullAllowedException("TenantId is required");
var clientSecret = builder.Configuration["ClientSecret"];

const string svName = "ServiceConnection";
const string ConnectionSettings = $"Connections:{svName}:Settings";
var inMemoryItems = new Dictionary<string, string?>
{
    { "TokenValidation:Audiences:0", clientId },
    { "TokenValidation:TenantId", tenantId },
    // ConnectionsMap with the ServiceConnection
    { "ConnectionsMap:0:ServiceUrlSettings", "*" },
    { "ConnectionsMap:0:Connection", svName },
    // ServiceConnection
    { $"{ConnectionSettings}:AuthorityEndpoint", "https://login.microsoftonline.com/" + tenantId },
    { $"{ConnectionSettings}:ClientId", clientId },
    { $"{ConnectionSettings}:Scopes:0", "https://api.botframework.com/.default" }
};
// will use workload if available
TokenCredential credentials = new DefaultAzureCredential();
// no secret so Federate
if (string.IsNullOrWhiteSpace(clientSecret))
{
    // ServiceConnection, for workload id
    inMemoryItems.Add($"{ConnectionSettings}:AuthType", "FederatedCredentials");
    inMemoryItems.Add($"{ConnectionSettings}:FederatedClientId", clientId);
}
else
{
    // ServiceConnection, for env with secret
    inMemoryItems.Add($"{ConnectionSettings}:AuthType", "ClientSecret");
    inMemoryItems.Add($"{ConnectionSettings}:ClientSecret", clientSecret);
    credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
}

builder.Configuration.AddInMemoryCollection(inMemoryItems);
builder.Services.AddSingleton(new GraphServiceClient(credentials));
builder.Services.AddTransient<RequestAndResponseLoggerHandler>();
builder.Services.AddTransient<IFileErrorManagerService, FileErrorManagerService>();
builder.Services.AddTransient<ITeamsManagerService, TeamsManagerService>();

builder.Services.AddAgentAspNetAuthentication(builder.Configuration);
builder.Services.AddMemoryCache();
// Add ApplicationOptions
builder.AddAgentApplicationOptions();
builder.AddAgent<FileErrorAgent>();
builder.Services.AddControllers(o =>
    {
        o.Conventions.Add(new HideChannelApi());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddSingleton<IMiddleware[]>(sp => [new CaptureMiddleware()]
);
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
});

// Register IStorage.  For development, MemoryStorage is suitable.
// For production Agents, persisted storage should be used so
// that state survives Agent restarts, and operate correctly
// in a cluster of Agent instances.
builder.Services.AddSingleton<IStorage, MemoryStorage>();


var app = builder.Build();
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();
app.UseSwagger(options =>
{
    options.RouteTemplate = $"{appPathPrefix}/swagger/{{documentname}}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "V1");
    c.RoutePrefix = $"{appPathPrefix}/swagger";
});

app.UsePathBase("/" + appPathPrefix);
app.UseRouting();
app.Run();