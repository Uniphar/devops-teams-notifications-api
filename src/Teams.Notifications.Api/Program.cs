using Teams.Notifications.Api.OpenapiTransformer;
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

var environmentSuffix = environment == "prod" ? string.Empty : $".{environment}";
//locally 
if (environment == "local") environmentSuffix = ".dev";

var apiUrl = new Uri($"https://api{environmentSuffix}.uniphar.ie/");


var jitterRandomizer = new Random();
builder.Services.AddHttpClient();
builder
    .Services
    .AddHttpClient(Consts.FrontgateApiClient, client => client.BaseAddress = apiUrl)
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(jitterRandomizer.Next(0, 100))))
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.CircuitBreakerAsync(
        5,
        TimeSpan.FromSeconds(30)
    ));
// will use workload if available
if (!string.IsNullOrWhiteSpace(clientSecret))
{
    credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        { "Connections:ServiceConnection:Settings:AuthType", "ClientSecret" },
        { "Connections:ServiceConnection:Settings:ClientId", clientId },
        { "Connections:ServiceConnection:Settings:TenantId", tenantId },
        { "Connections:ServiceConnection:Settings:ClientSecret", clientSecret },
        { "Connections:ServiceConnection:Settings:Scopes:0", "https://api.botframework.com/.default" }
    });
}
else
{
    var federatedTokenFile = builder.Configuration["AZURE_FEDERATED_TOKEN_FILE"] ?? throw new NoNullAllowedException("Token file is required");
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        { "Connections:ServiceConnection:Settings:AuthType", "WorkloadIdentity" },
        { "Connections:ServiceConnection:Settings:ClientId", clientId },
        { "Connections:ServiceConnection:Settings:TenantId", tenantId },
        { "Connections:ServiceConnection:Settings:FederatedTokenFile", federatedTokenFile },
        { "Connections:ServiceConnection:Settings:Scopes:0", "https://api.botframework.com/.default" }
    });
}

builder.Services.AddSingleton(credentials);
builder.Services.AddSingleton(new GraphServiceClient(credentials));
builder.Services.AddTransient<RequestAndResponseLoggerHandler>();
builder.Services.AddTransient<ICardManagerService, CardManagerService>();
builder.Services.AddTransient<ITeamsManagerService, TeamsManagerService>();
builder.Services.AddTransient<IFrontgateApiService, FrontgateApiService>();
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
        o.Conventions.Add(new HideChannelApi());
        o.Conventions.Add(new GlobalRouteConvention(appPathPrefix));
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
if (environment != "local")
    // key vault is required for ApplicationInsights, since it needs the connection string, but locally we will remove it
    builder.Configuration.AddAzureKeyVault(new Uri($"https://uni-devops-app-{environment}-kv.vault.azure.net/"), credentials);

builder.Services.AddSingleton<IMiddleware[]>(_ => [new CaptureMiddleware()]);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<AddExternalDocsTransformer>();

    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// Register IStorage.  For development, MemoryStorage is suitable.
// For production Agents, persisted storage should be used so
// that state survives Agent restarts, and operate correctly
// in a cluster of Agent instances.
builder.Services.AddSingleton<IStorage, MemoryStorage>();

// Configure OpenTelemetry
builder.RegisterOpenTelemetry(appPathPrefix);

var app = builder.Build();
app.MapHealthChecks("/health");
app.MapOpenApi(appPathPrefix + "/swagger/{documentName}/openapi.json");
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/openapi.json", "V1");
    c.RoutePrefix = $"{appPathPrefix}/swagger";
});
app.MapControllers();
app.Run();