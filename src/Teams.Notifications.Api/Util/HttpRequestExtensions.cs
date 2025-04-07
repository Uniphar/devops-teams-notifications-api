using System.Buffers;
using Microsoft.AspNetCore.Http.Extensions;
using NeoSmart.AsyncLock;

namespace Teams.Notifications.Api.Util
{
	public static class HttpRequestExtensions
	{
		private static ConcurrentDictionary<string, AsyncLock> FileLocks { get; } = new();

		public static async Task AppendToHttpFile(this HttpRequest request, string path)
		{
			var fileLock = FileLocks.GetOrAdd(path, _ => new AsyncLock());

			using (await fileLock.LockAsync())
			{
				using var fileStream = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);
				using var writer = new StreamWriter(fileStream);

				await writer.WriteAsync("\n\n###\n\n");
				await writer.WriteLineAsync($"{request.Method} {request.GetEncodedUrl()}");

				foreach (var (header, values) in request.Headers)
					foreach (var value in values)
						await writer.WriteLineAsync($"{header}: {value}");

				var buffer = ArrayPool<char>.Shared.Rent(2048);
				try
				{
					await writer.WriteLineAsync();
					using var reader = new StreamReader(request.Body);
					while (await reader.ReadBlockAsync(buffer) is > 0 and var charsRead)
						await writer.WriteAsync(buffer.AsMemory().Slice(0, charsRead));
				}
				finally
				{
					ArrayPool<char>.Shared.Return(buffer);
				}
			}
		}
	}
}
