using System.Text.Json.Serialization;
using Microsoft.Graph.Beta.Models;

namespace Teams.Cards.Api;

public sealed class ChannelService(GraphServiceClient GraphClient, [FromKeyedServices("channels")] Container ChannelContainer, CardService CardService, BotClientFactory BotClientFactory)
{
	private ConcurrentDictionary<(string teamId, string channelId), StoredChannel> ChannelCache { get; } = new();

	private async ValueTask<StoredChannel?> GetStoredChannel(string teamId, string channelId, CancellationToken cancellationToken = default)
	{
		if (ChannelCache.TryGetValue((teamId, channelId), out var cachedChannel))
			return cachedChannel;

		var storedChannel = await ChannelContainer.GetByIdAsync<StoredChannel>($"{teamId}/{channelId}", new PartitionKey(teamId), cancellationToken);
		if (storedChannel is null)
			return null;

		return ChannelCache[(teamId, channelId)] = storedChannel;
	}

	public async ValueTask<Channel?> GetChannel(string teamId, string channelId)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(teamId);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(channelId);

		var storedChannel = await GetStoredChannel(teamId, channelId);
		if (storedChannel is null)
			return null;

		return new Channel
		{
			TeamId = storedChannel.TeamId,
			ChannelId = storedChannel.Id,
			OwningApplication = storedChannel.OwningApplication
		};
	}

	public async Task CreateChannel(string teamId, string channelId, string channelName, Guid owningApplication, string? description = default)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(teamId);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(channelId);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(channelName);
		
		if (await GetStoredChannel(teamId, channelId) is not null)
			throw new InvalidOperationException("Channel already exists");

		if (await GraphClient.Teams[teamId].Channels.NameExistsAsync(channelName))
			throw new InvalidOperationException("Channel with that name already exists");

		var newChannel = await GraphClient.Teams[teamId].Channels.PostAsync(new Microsoft.Graph.Beta.Models.Channel
		{
			DisplayName = channelName.Trim(),
			Description = description?.Trim(),
			LayoutType = Microsoft.Graph.Beta.Models.ChannelLayoutType.Chat,
			MembershipType = Microsoft.Graph.Beta.Models.ChannelMembershipType.Standard,
			ModerationSettings = new ChannelModerationSettings
			{
				AllowNewMessageFromBots = true,
				AllowNewMessageFromConnectors = false,
				UserNewMessageRestriction = UserNewMessageRestriction.Moderators
			},
			Tabs = []
		});

		if (newChannel is null)
			throw new InvalidOperationException("Failed to create new channel");

		var newStoredChannel = await ChannelContainer.CreateItemAsync(new StoredChannel
		{
			TeamId = teamId,
			ChannelTeamsId = newChannel.Id!,
			Id = channelId,
			OwningApplication = owningApplication
		});

		ChannelCache[(teamId, channelId)] = newStoredChannel;
	}

	public async Task UpdateChannel(string teamId, string channelId, string? channelName = default, string? description = default)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(teamId);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(channelId);

		if (channelName is not null && string.IsNullOrWhiteSpace(channelName))
			throw new ArgumentNullException("channelName must not be empty");

		if (await GetStoredChannel(teamId, channelId) is not StoredChannel storedChannel)
			throw new InvalidOperationException("Channel doesn't exist");

		var result = await GraphClient.Teams[teamId].Channels[storedChannel.Id].PatchAsync(new Microsoft.Graph.Beta.Models.Channel
		{
			DisplayName = channelName?.Trim(),
			Description = description?.Trim()
		});

		if (result is null)
			throw new InvalidOperationException("Failed to update channel");
	}

	public async Task DeleteChannel(string teamId, string channelId)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(teamId);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(channelId);

		if (await GetStoredChannel(teamId, channelId) is not StoredChannel storedChannel)
			throw new InvalidOperationException("Channel doesn't exist");

		await GraphClient.Teams[teamId].Channels[storedChannel.ChannelTeamsId].DeleteAsync();
		await ChannelContainer.DeleteItemAsync<StoredChannel>(storedChannel.Id, new PartitionKey(storedChannel.TeamId));
		await CardService.Cleanup(teamId, channelId);
	}

	public async Task UpdateChannelServiceUri(string teamId, string channelTeamsId, Uri serviceUri)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(teamId);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(channelTeamsId);

		var storedChannel = await ChannelContainer.QueryItems<StoredChannel>(("channelTeamsId", channelTeamsId)).SingleOrDefaultAsync();
		if (storedChannel is null)
			throw new InvalidOperationException("Channel not found.");

		var updatedChannel = await ChannelContainer.PatchItemAsync<StoredChannel>(storedChannel.Id, new PartitionKey(storedChannel.TeamId), [PatchOperation.Set("/serviceUri", serviceUri)]);
		ChannelCache[(updatedChannel.Resource.TeamId, updatedChannel.Resource.Id)] = updatedChannel.Resource;
	}

	public async ValueTask<IBotClient> GetBotClient(string teamId, string channelId)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(teamId);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(channelId);

		var storedChannel = await GetStoredChannel(teamId, channelId);
		if (storedChannel is null)
			throw new InvalidOperationException("Channel doesn't exist");

		return storedChannel.ServiceUri is not null
			? BotClientFactory.GetClientForServiceUri(storedChannel.ServiceUri)
			: BotClientFactory.DefaultClient;
	}

	private sealed record StoredChannel
	{
		public required string TeamId { get; init; }
		public required string ChannelTeamsId { get; init; }
		public required string Id { get; init; }
		public required Guid OwningApplication { get; init; }

		public Uri? ServiceUri { get; init; }

		[JsonPropertyName("_etag")]
		public string? ETag { get; init; }
	}
}