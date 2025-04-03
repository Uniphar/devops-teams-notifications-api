using Azure.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Teams.Cards.BotFramework;

public static class ServiceRegistration
{
	public static IServiceCollection AddBotFramework(this IServiceCollection services)
		=> services.AddSingleton<BotClientFactory>();

	public static IServiceCollection AddBotFramework(this IServiceCollection services, TokenCredential tokenCredential)
		=> services.AddSingleton(_ => new BotClientFactory(tokenCredential));
}