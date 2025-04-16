using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Connector;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph.Beta;
using Teams.Notifications.Api;
using Teams.Notifications.Api.Agents;
using Teams.Notifications.Api.Extensions;
using Teams.Notifications.Api.Middleware;
using Teams.Notifications.Api.Services;
using Teams.Notifications.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// this is what the bot is communicating on
builder.Services.AddHttpClient(typeof(RestChannelServiceClientFactory).FullName!).AddHttpMessageHandler<RequestAndResponseLoggerHandler>();
// Register Semantic Kernel
builder.Services.AddKernel();

var credentials = new DefaultAzureCredential();
builder.Services.AddSingleton<TokenCredential>(credentials);
builder.Services.AddSingleton(serviceProvider =>
{
    var credential = serviceProvider.GetRequiredService<TokenCredential>();
    return new GraphServiceClient(credential);
});
builder.Services.AddTransient<ICardStatesService, CardStatesService>();
builder.Services.AddTransient<RequestAndResponseLoggerHandler>();
builder.Services.AddTransient<IFileErrorManagerService, FileErrorManagerService>();
builder.Services.AddTransient<ITeamsChannelMessagingService, TeamsChannelMessagingService>();
builder.Services.AddTransient<ITeamsManagerService, TeamsManagerService>();

builder.Services.AddAgentAspNetAuthentication(builder.Configuration);
builder.Services.AddMemoryCache();
builder.AddAgent<FileErrorAgent>();
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddControllers();
builder.Services.AddSingleton<IMiddleware[]>(sp => [new CaptureMiddleware()]
);
// Add ApplicationOptions
builder.Services.AddTransient(sp => new AgentApplicationOptions(sp.GetRequiredService<IStorage>())
{
    StartTypingTimer = false,
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => "Microsoft Agents SDK Sample - StreamingMessageAgent");
    app.UseDeveloperExceptionPage();
    app.MapControllers().AllowAnonymous();
}
else
{
    app.MapControllers();
}

app.Run();

