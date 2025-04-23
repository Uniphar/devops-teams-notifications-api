using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Azure.Identity;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph.Beta;
using Microsoft.OpenApi.Models;
using Teams.Notifications.Api;
using Teams.Notifications.Api.Agents;
using Teams.Notifications.Api.DelegatingHandlers;
using Teams.Notifications.Api.Extensions;
using Teams.Notifications.Api.Middlewares;
using Teams.Notifications.Api.Services;
using Teams.Notifications.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// this is what the bot is communicating on
builder.Services.AddHttpClient(typeof(RestChannelServiceClientFactory).FullName!).AddHttpMessageHandler<RequestAndResponseLoggerHandler>();
// Register Semantic Kernel
builder.Services.AddKernel();
var environment = builder.Environment.EnvironmentName ?? throw new NoNullAllowedException("ASPNETCORE_ENVIRONMENT environment variable has to be set.");
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://uni-devops-app-{environment}-kv.vault.azure.net/"),
    new DefaultAzureCredential());
// Values from app registration
var clientId = builder.Configuration["ClientId"] ?? throw new NoNullAllowedException("ClientId is required");
var tenantId = builder.Configuration["TenantId"] ?? throw new NoNullAllowedException("TenantId is required");
var clientSecret = builder.Configuration["ClientSecret"]?? throw new NoNullAllowedException("ClientSecret is required");
const string svName = "ServiceConnection";
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    { "TokenValidation:Audiences:0", clientId },
    { "TokenValidation:TenantId", tenantId },
    // ConnectionsMap with the ServiceConnection
    { "ConnectionsMap:0:ServiceUrlSettings", "*" },
    { "ConnectionsMap:0:Connection", svName },
    // ServiceConnection
    { $"Connections:{svName}:Settings:AuthType", "ClientSecret" },
    { $"Connections:{svName}:Settings:AuthorityEndpoint", "https://login.microsoftonline.com/" + tenantId },
    { $"Connections:{svName}:ClientId", clientId },
    { $"Connections:{svName}:ClientSecret", clientSecret },
    { $"Connections:{svName}:Settings:Scopes:0", "https://api.botframework.com/.default" }
});
var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
builder.Services.AddSingleton(new GraphServiceClient(clientSecretCredential));
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