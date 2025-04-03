namespace Teams.Cards.BotFramework.Serialization;

internal sealed class IdConverter : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericOfType(typeof(Id<>));

	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		=> typeof(IdConverter<>).InstantiateGeneric<JsonConverter>(typeToConvert);
}

file sealed class IdConverter<TKey> : JsonConverter<Id<TKey>> where TKey : notnull
{
	public override Id<TKey>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.StartObject or JsonTokenType.String or JsonTokenType.Number)
			throw new InvalidOperationException("Expected a value or object");

		if (reader.TokenType is not JsonTokenType.StartObject)
			return new Id<TKey>(options.GetConverter<TKey>().Read(ref reader, typeof(TKey), options)!);

		while (true)
		{
			reader.Read();
			if (reader.TokenType is JsonTokenType.EndObject)
				break;

			if (reader.TokenType is not JsonTokenType.PropertyName)
				throw new InvalidOperationException("Expected property name");

			if (reader.ValueTextEquals("id"))
			{
				reader.Read();

				var value = new Id<TKey>(options.GetConverter<TKey>().Read(ref reader, typeof(TKey), options)!);

				reader.SkipRemainingProperties();
				return value;
			}
			else
			{
				reader.SkipProperty();
			}
		}

		throw new InvalidOperationException("Could not find `id` property");
	}

	public override void Write(Utf8JsonWriter writer, Id<TKey> value, JsonSerializerOptions options)
	{
		options.GetConverter<TKey>().Write(writer, value, options);
	}
}