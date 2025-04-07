
using System.Diagnostics.CodeAnalysis;
using Teams.Notifications.Api.Channel;

namespace Teams.Notifications.Api.Card;

public sealed class CardService(GraphServiceClient GraphClient, [FromKeyedServices("cards")] Container CardContainer, Func<ChannelService> ChannelServiceFunc)
{
	[field: NotNull]
	private ChannelService ChannelService { get => field ??= ChannelServiceFunc(); }

	public async Task UpsertCard(string teamId, string channelId, string cardId, JsonDocument content)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(teamId);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(channelId);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(cardId);
		ArgumentNullException.ThrowIfNull(content);



		var botClient = ChannelService.GetBotClient(teamId, channelId);


	}



	internal async Task Cleanup(string teamId, string channelId)
	{
		await CardContainer.DeleteAllItemsByPartitionKeyStreamAsync(new PartitionKey($"{teamId}/{channelId}"));
	}
}