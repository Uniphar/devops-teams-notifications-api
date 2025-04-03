using System.Buffers;

namespace Teams.Cards.BotFramework.Serialization;

/// <summary>
/// When applied to a property, the deserialized instances of the property type will be cached
/// and future deserializations of the same string will create no garbage and return the cached
/// instance.
/// </summary>
/// <remarks>
///		<para>Use with care - the deserialized instances will live for the length of the program.</para>
/// 
///		<para>
///			Currently supported types:
///			<list type="bullet">
///				<item><seealso cref="Uri"/></item>
///			</list>
///		</para>
/// </remarks>
internal sealed class CacheDeserializedInstancesAttribute : JsonConverterAttribute
{
	public override JsonConverter? CreateConverter(Type typeToConvert)
	{
		if (typeToConvert == typeof(Uri))
			return new CacheDeserializedInstancesConverter<Uri>();
		else
			throw new NotSupportedException($"Cannot cache type `{typeToConvert.FullName}` - try adding a converter to `{nameof(CacheDeserializedInstancesAttribute)}.{nameof(CreateConverter)}`.");
	}
}

file sealed class CacheDeserializedInstancesConverter<T> : JsonConverter<T>
{
	private static ConcurrentDictionary<string, T?> Cache { get; } = new();

	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
			throw new InvalidOperationException("Not on a string value");

		using var charBuffer = ArrayPool<byte>.Shared.RentBuffer<char>((int)reader.GetUtf8StringLength());
		var valueCharBuffer = reader.CopyStringToBuffer(charBuffer);

		if (Cache.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(valueCharBuffer, out var existingValue))
			return existingValue;

		var valueString = new string(valueCharBuffer);
		var deserializedInstance = options.GetConverter<T>().Read(ref reader, typeToConvert, options);
		return Cache.GetOrAdd(valueString, deserializedInstance);
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		// Just pass through, we don't do anything special on write
		options.GetConverter<T>().Write(writer, value, options);
	}
}