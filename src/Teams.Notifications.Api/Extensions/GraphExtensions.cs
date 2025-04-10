using System.Threading.Tasks;
using Microsoft.Graph.Beta.Teams.Item.Channels;

namespace Teams.Notifications.Api.Extensions;

internal static class GraphExtensions
{
    public static async Task<bool> NameExistsAsync(this ChannelsRequestBuilder channels, string name)
    {
        var response = await channels.GetAsync(r => r.QueryParameters.Filter = $"displayName eq '{name.Replace("'", "''")}'");
        return response?.OdataCount > 0;
    }
}