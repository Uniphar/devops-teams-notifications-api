using System.Data;
using System.Text.Encodings.Web;
using Azure.Core;
using Azure.Identity;
using Teams.Cards.Api;

[assembly: ApiController]

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment.EnvironmentName ?? throw new NoNullAllowedException("ASPNETCORE_ENVIRONMENT environment variable has to be set.");

builder.Services.AddSingleton<TokenCredential>(_ => builder.Environment.EnvironmentName is "dev"
	? new EnvironmentCredential()
	: new WorkloadIdentityCredential()
);

builder.Services.AddHttpClient();


builder.Services.AddSingleton(serviceProvider =>
{
	var credential = serviceProvider.GetRequiredService<TokenCredential>();
	return new GraphServiceClient(credential);
});

await builder.Services.AddCosmos($"https://uni-devops-{environment}-cosmos.documents.azure.com/")
	.AddContainer(diKey: "channels", "devops", "teams-cards-api-channels", partitionKeyPath: "/teamId")
	.AddContainer(diKey: "cards", "devops", "teams-cards-api-cards", partitionKeyPath: "/channelId");

builder.Services.AddBotFramework();

builder.Services.AddControllers()
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

app.MapControllers();

app.Run();

/*await graphUserClient.PostJsonAsync("teams/70db5c22-571f-47e8-a2d7-33d40ddc1aa8/installedApps", new Dictionary<string, object>
	{
		{ "teamsApp@odata.bind", "https://graph.microsoft.com/beta/appCatalogs/teamsApps/4c1c145e-eb0d-47cd-a2f1-02df93cf2c92" },
		{ "consentedPermissionSet", new
		{
			resourceSpecificPermissions = new[]
			{
				new { permissionType = "application", permissionValue = "Channel.Create.Group" },
				new { permissionType = "application", permissionValue = "TeamsActivity.Send.Group" },
				new { permissionType = "application", permissionValue = "TeamMember.Read.Group" },
				new { permissionType = "application", permissionValue = "Member.Read.Group" },
				new { permissionType = "application", permissionValue = "Owner.Read.Group" },
				new { permissionType = "application", permissionValue = "ChannelSettings.ReadWrite.Group" },
				new { permissionType = "application", permissionValue = "ChannelMessage.Read.Group" },
				new { permissionType = "application", permissionValue = "ChannelMessage.Send.Group" },
				new { permissionType = "application", permissionValue = "ChannelSettings.Read.Group" },
				new { permissionType = "application", permissionValue = "Channel.Delete.Group" },
				new { permissionType = "application", permissionValue = "TeamsActivity.Send.User" }
			}
		} }
	}).Dump();*/