using System.Data;
using System.Reflection;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Teams.Notifications.Api;
using Teams.Notifications.Api.Agents;
using Teams.Notifications.Api.DelegatingHandlers;
using Teams.Notifications.Api.Middlewares;
using Teams.Notifications.Api.Services;
using IMiddleware = Microsoft.Agents.Builder.IMiddleware;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

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
var inMemoryItems = new Dictionary<string, string?>
{
    { "TokenValidation:Audiences:0", clientId },
    { "TokenValidation:TenantId", tenantId },
    // ConnectionsMap with the ServiceConnection
    { "ConnectionsMap:0:ServiceUrlSettings", "*" },
    { "ConnectionsMap:0:Connection", svName },
    // ServiceConnection
    { $"Connections:{svName}:Settings:AuthorityEndpoint", "https://login.microsoftonline.com/" + tenantId },
    { $"Connections:{svName}:ClientId", clientId },
    { $"Connections:{svName}:Settings:Scopes:0", "https://api.botframework.com/.default" }
};
// will use workload if available
TokenCredential credentials = new DefaultAzureCredential();
// no secret so fedrate
if (string.IsNullOrWhiteSpace(clientSecret))
{
    // ServiceConnection, for workload id
    inMemoryItems.Add($"Connections:{svName}:Settings:AuthType", "FederatedCredentials");
    inMemoryItems.Add($"Connections:{svName}:FederatedClientId", clientId);
}
else
{
    // ServiceConnection, for env with secret
    inMemoryItems.Add($"Connections:{svName}:Settings:AuthType", "ClientSecret");
    inMemoryItems.Add($"Connections:{svName}:ClientSecret", clientSecret);
    credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
}

builder.Configuration.AddInMemoryCollection(inMemoryItems);
builder.Services.AddSingleton(new GraphServiceClient(credentials));
builder.Services.AddTransient<RequestAndResponseLoggerHandler>();
builder.Services.AddTransient<IFileErrorManagerService, FileErrorManagerService>();
builder.Services.AddTransient<ITeamsManagerService, TeamsManagerService>();

builder.Services.AddAgentAspNetAuthentication(builder.Configuration);
builder.Services.AddMemoryCache();
builder.AddAgent<FileErrorAgent>();
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddControllers(o => { o.Conventions.Add(new HideChannelApi()); });
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

// Add ApplicationOptions
builder.Services.AddTransient(sp => new AgentApplicationOptions(sp.GetRequiredService<IStorage>())
{
    StartTypingTimer = false
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapControllers().AllowAnonymous();
}


app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.Run();