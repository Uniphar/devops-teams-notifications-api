using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Teams.Cards.BotFramework.Utils;

internal ref struct ValueStringBuilder : IDisposable
{
	private Span<char> chars;
	private ArrayPool<byte>? arrayPool;
	private byte[]? buffer;
	private int length;

	private int AvailableCapacity => chars.Length - length;
	private Span<char> AvailableSpan => chars.Slice(length);

	public ValueStringBuilder(Span<char> chars)
	{
		this.chars = chars;
		this.arrayPool = null;
		this.buffer = null;
		this.length = 0;
	}

	public ValueStringBuilder(ArrayPool<byte> arrayPool, int minimumCapacity)
	{
		this.arrayPool = arrayPool;
		buffer = arrayPool.Rent(minimumCapacity);
		chars = buffer.Cast<char>();
		length = 0;
	}

	private void ExpandBuffer(int newMinimumCapacity)
	{
		var arrayPool = this.arrayPool ??= ArrayPool<byte>.Shared;
		var oldBuffer = buffer;
		var oldChars = chars;

		var newBufferLength = (int)BitOperations.RoundUpToPowerOf2((uint)newMinimumCapacity);
		var newBuffer = buffer = arrayPool.Rent(newBufferLength);
		var newChars = chars = newBuffer.Cast<char>();
		
		chars.Slice(0, length).CopyTo(newBuffer.Cast<char>());

		if (oldBuffer is not null)
			arrayPool.Return(oldBuffer);
	}

	private void EnsureCapacity(int neededCapacity)
	{
		if (neededCapacity < AvailableCapacity)
			return;
		else
			ExpandBuffer(length + neededCapacity);
	}

	public void Append(scoped ReadOnlySpan<char> chars)
	{
		if (chars.Length == 0)
			return;

		EnsureCapacity(chars.Length);

		chars.CopyTo(AvailableSpan);
		length += chars.Length;
	}

	public void Append(ReadOnlyMemory<char> chars) => Append(chars.Span);

	public void Append(string? str) => Append(str.AsSpan());

	public void Append(char c)
	{
		EnsureCapacity(1);
		AvailableSpan[0] = c;
		length++;
	}

	public void Append(char c, int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		EnsureCapacity(count);
		AvailableSpan.Slice(0, count).Fill(c);
		length += count;
	}

	public void Append(InterpolatedStringHandler interpolatedStringHandler)
	{
	}

	public void Append<T>(T value)
	{
		if (value is not IFormattable)
		{
			Append(value?.ToString());
			return;
		}

		if (typeof(T).IsEnum)
		{
			int charsWritten;
			while (!TryFormatUnconstrained(default, value, AvailableSpan, out charsWritten))
				EnsureCapacity(charsWritten * 2);

			length += charsWritten;
		}

		if (value is ISpanFormattable)
		{
			int charsWritten;
			while (!((ISpanFormattable)value).TryFormat(AvailableSpan, out charsWritten, default, default))
				EnsureCapacity(charsWritten * 2);

			length += charsWritten;
			return;
		}

		Append(((IFormattable)value).ToString(format: null, null));

		[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "TryFormatUnconstrained")]
		static extern bool TryFormatUnconstrained<TEnum>(Enum _, TEnum value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default);
	}

	public void Append<T>(T value, string? format)
	{
		if (value is not IFormattable)
		{
			Append(value?.ToString());
			return;
		}

		if (typeof(T).IsEnum)
		{
			int charsWritten;
			while (!TryFormatUnconstrained(default, value, AvailableSpan, out charsWritten, format))
				EnsureCapacity(charsWritten * 2);

			length += charsWritten;
		}

		if (value is ISpanFormattable)
		{
			int charsWritten;
			while (!((ISpanFormattable)value).TryFormat(AvailableSpan, out charsWritten, format, default))
				EnsureCapacity(length + charsWritten * 2);

			length += charsWritten;
			return;
		}

		Append(((IFormattable)value).ToString(format, null));

		[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "TryFormatUnconstrained")]
		static extern bool TryFormatUnconstrained<TEnum>(Enum _, TEnum value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default);
	}

	public void Dispose()
	{
		if (arrayPool is null || buffer is null)
			return;
		
		arrayPool.Return(buffer);
		buffer = default;
		chars = default;
		length = default;
	}

	[InterpolatedStringHandler]
	public ref struct InterpolatedStringHandler
	{
		private ValueStringBuilder builder;

		public InterpolatedStringHandler(int literalLength, int formattedCount, ValueStringBuilder builder)
		{
			var guessedLengthForHoles = formattedCount * 11;
			builder.EnsureCapacity(literalLength + guessedLengthForHoles);
			this.builder = builder;
		}

		public void AppendLiteral(string value) => builder.Append(value);
		public void AppendFormatted<T>(T value) => builder.Append(value);
		public void AppendFormatted<T>(T value, string? format) => builder.Append(value, format);

		public void AppendFormatted<T>(T value, int alignment)
		{
			int startingPos = builder.length;
			AppendFormatted(value);
			if (alignment != 0)
				AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
		}

		public void AppendFormatted<T>(T value, int alignment, string? format)
		{
			int startingPos = builder.length;
			AppendFormatted(value, format);
			if (alignment != 0)
				AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
		}

		public void AppendFormatted(scoped ReadOnlySpan<char> value) => builder.Append(value);

		public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
		{
			bool leftAlign = false;
			if (alignment < 0)
			{
				leftAlign = true;
				alignment = -alignment;
			}

			int paddingRequired = alignment - value.Length;
			if (paddingRequired <= 0)
			{
				// The value is as large or larger than the required amount of padding,
				// so just write the value.
				AppendFormatted(value);
				return;
			}

			// Write the value along with the appropriate padding.
			builder.EnsureCapacity(value.Length + paddingRequired);

			if (leftAlign)
			{
				builder.Append(value);
				builder.Append(' ', paddingRequired);
			}
			else
			{
				builder.Append(' ', paddingRequired);
				builder.Append(value);
			}
		}

		public void AppendFormatted(string? value) => builder.Append(value);

		public void AppendFormatted(string? value, int alignment = 0, string? format = null)
			=> AppendFormatted<string?>(value, alignment, format);

		public void AppendFormatted(object? value, int alignment = 0, string? format = null)
			=> AppendFormatted<object?>(value, alignment, format);

		private void AppendOrInsertAlignmentIfNeeded(int startingPos, int alignment)
		{
			Debug.Assert(startingPos >= 0 && startingPos <= builder.length);
			Debug.Assert(alignment != 0);

			int charsWritten = builder.length - startingPos;

			bool leftAlign = false;
			if (alignment < 0)
			{
				leftAlign = true;
				alignment = -alignment;
			}

			int paddingNeeded = alignment - charsWritten;
			if (paddingNeeded <= 0)
				return;

			builder.EnsureCapacity(paddingNeeded);

			if (leftAlign)
			{
				builder.Append(' ', paddingNeeded);
			}
			else
			{
				builder.chars.Slice(startingPos, charsWritten).CopyTo(builder.chars.Slice(startingPos + paddingNeeded));
				builder.chars.Slice(startingPos, paddingNeeded).Fill(' ');
			}

			builder.length += paddingNeeded;
		}
	}
}
