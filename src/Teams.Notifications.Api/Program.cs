using Azure.Core;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph.Beta;
using Teams.Notifications.Api.AgentApplication;
using Teams.Notifications.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

// Register Semantic Kernel
builder.Services.AddKernel();

builder.Services.AddSingleton(serviceProvider =>
{
    var credential = serviceProvider.GetRequiredService<TokenCredential>();
    return new GraphServiceClient(credential);
});

builder.Services.AddAgentAspNetAuthentication(builder.Configuration);

builder.AddAgent<FileErrorAgent>();
builder.Services.AddSingleton<IStorage, MemoryStorage>();
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

