using System.Text.Encodings.Web;
using System.Text.Json;
using Azure.Core;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph.Beta;
using Teams.Notifications.Api.AgentApplication;
using Teams.Notifications.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddHttpClient();

// Register Semantic Kernel
builder.Services.AddKernel();

builder.Services.AddSingleton(serviceProvider =>
{
    var credential = serviceProvider.GetRequiredService<TokenCredential>();
    return new GraphServiceClient(credential);
});

builder
    .Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddBotAspNetAuthentication(builder.Configuration);

builder.AddAgent<FileErrorAgent>();
builder.Services.AddSingleton<IStorage, MemoryStorage>();
// Add ApplicationOptions
builder.Services.AddTransient(sp => new AgentApplicationOptions(sp.GetRequiredService<IStorage>())
{
    StartTypingTimer = false,
});

/*builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("TeamsJwt", options =>
    {
        options.MetadataAddress = builder.Environment.EnvironmentName is "dev" && builder.Configuration.GetValue<bool?>("UseBotEmulator") == true
            ? "https://login.microsoftonline.com/botframework.com/v2.0/.well-known/openid-configuration"
            : "https://login.botframework.com/v1/.well-known/openidconfiguration";

        options.RefreshInterval = TimeSpan.FromDays(1);
        options.RefreshOnIssuerKeyNotFound = true;
        options.RequireHttpsMetadata = true;

        options.Audience = GetEnvironmentVariable("AZURE_CLIENT_ID");
        options.TokenValidationParameters.ValidateAudience = true;

        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);

        options.IncludeErrorDetails = environment is "dev";

        //TODO: Validate the serviceUrl against the body -_-
    });
*/
/*builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    { "InternalJwt:Instance", "https://login.microsoftonline.com/" },
    { "InternalJwt:TenantId", GetEnvironmentVariable("AZURE_TENANT_ID") },
    { "InternalJwt:ClientId", GetEnvironmentVariable("AZURE_CLIENT_ID") }
});*/

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("InternalJwt"));

//builder.Services.AddAuthorizationBuilder().AddPolicy("InternalJwtPolicy", policy => policy.));

var app = builder.Build();

//app.UseAuthorization();

app.MapGet("/", () => "Example Agents");
app.UseDeveloperExceptionPage();


app.MapControllers();

app.Run();

