using System.Buffers;
using System.Runtime.InteropServices;

namespace Teams.Cards.BotFramework;

internal static class SpanExtensions
{
	public static Span<T> AsSpan<T>(this byte[] bytes) where T : unmanaged => MemoryMarshal.Cast<byte, T>(bytes);
	public static ReadOnlySpan<T> AsReadOnlySpan<T>(this byte[] bytes) where T : unmanaged => MemoryMarshal.Cast<byte, T>(bytes);
	public static Span<T> Cast<T>(this Span<byte> bytes) where T : unmanaged => MemoryMarshal.Cast<byte, T>(bytes);
	public static ReadOnlySpan<T> Cast<T>(this ReadOnlySpan<byte> bytes) where T : unmanaged => MemoryMarshal.Cast<byte, T>(bytes);

	public static RentedBuffer<T> RentBuffer<T>(this ArrayPool<byte> pool, int minimumSize) where T : unmanaged	=> new RentedBuffer<T>(pool, minimumSize);

	public unsafe readonly ref struct RentedBuffer<T>(ArrayPool<byte> ArrayPool, int minimumSize) : IDisposable
		where T : unmanaged
	{
		private byte[] Buffer { get; } = ArrayPool.Rent(minimumSize * sizeof(T));

		public static implicit operator Span<T>(RentedBuffer<T> buffer) => buffer.Buffer.AsSpan().Cast<T>();

		public void Dispose()
		{
			if (Buffer is not null)
				ArrayPool.Return(Buffer);
		}
	}
}