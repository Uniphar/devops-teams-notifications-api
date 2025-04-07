using Microsoft.IO;

namespace Teams.Notifications.Api.Util;

internal static class MemoryStreams
{
	private static RecyclableMemoryStreamManager MemoryStreamManager = new(new RecyclableMemoryStreamManager.Options
	{
		BlockSize = 4096,
		ZeroOutBuffer = true
	});

	public static async ValueTask<RecyclableMemoryStream> ToMemoryStreamAsync(this Stream stream)
	{
		var newStream = stream.CanSeek
			? MemoryStreamManager.GetStream(tag: null, requiredSize: stream.Length)
			: MemoryStreamManager.GetStream();

		await stream.CopyToAsync(newStream);
		newStream.Position = 0;
		return newStream;
	}

	public static RecyclableMemoryStream Create() => MemoryStreamManager.GetStream();
}
