namespace Teams.Cards.BotFramework.Serialization;

internal static class JsonExtensions
{
	public static string? GetStringPropertyOrDefault(this JsonElement elem, string propName)
	{
		return elem.TryGetProperty(propName, out var prop) && prop.ValueKind is JsonValueKind.String
				? prop.GetString()
				: default;
	}

	public static long GetUtf8StringLength(this ref Utf8JsonReader reader)
	{
		return reader.HasValueSequence
			? reader.ValueSequence.Length
			: reader.ValueSpan.Length;
	}

	public static Span<char> CopyStringToBuffer(this Utf8JsonReader reader, Span<char> buffer)
	{
		var written = reader.CopyString(buffer);
		return buffer.Slice(0, written);
	}

	public static void WritePropertyIfNotIgnored<T>(this Utf8JsonWriter writer, string name, T value, JsonSerializerOptions options)
	{
		if (options.ShouldIgnore(value))
			return;

		writer.WritePropertyName(name);
		options.GetConverter<T>().Write(writer, value, options);
	}

	public static T? ReadPropertyValue<T>(this ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		reader.Read();
		return options.GetConverter<T>().Read(ref reader, typeof(T), options);
	}

	public static void SkipProperty(this ref Utf8JsonReader reader)
	{
		if (reader.TokenType is JsonTokenType.PropertyName)
			reader.Read();

		reader.Skip();
	}

	public static void SkipRemainingProperties(this ref Utf8JsonReader reader)
	{
		while (true)
		{
			reader.Read();
			if (reader.TokenType is JsonTokenType.PropertyName)
				reader.SkipProperty();
			else if (reader.TokenType is JsonTokenType.EndObject)
				return;
			else
				throw new InvalidOperationException("Invalid reader position for skipping properties");
		}
	}

	public static JsonConverter<T> GetConverter<T>(this JsonSerializerOptions options)
		=> (JsonConverter<T>)options.GetConverter(typeof(T));

	public static bool ShouldIgnore(this JsonSerializerOptions options, Type propertyType, object? value)
	{
		return options.DefaultIgnoreCondition switch
		{
			JsonIgnoreCondition.Never => false,

			JsonIgnoreCondition.WhenWritingNull when !propertyType.IsValueType => value is null,
			JsonIgnoreCondition.WhenWritingNull when propertyType.IsValueType => false,

			JsonIgnoreCondition.WhenWritingDefault when !propertyType.IsValueType => value is null,
			JsonIgnoreCondition.WhenWritingDefault when propertyType.IsValueType => IsDefaultValue(propertyType, value),

			_ => false
		};
	}

	public static bool ShouldIgnore<T>(this JsonSerializerOptions options, T? value)
	{
		return options.DefaultIgnoreCondition switch
		{
			JsonIgnoreCondition.Never => false,

			JsonIgnoreCondition.WhenWritingNull when !typeof(T).IsValueType => value is null,
			JsonIgnoreCondition.WhenWritingNull when typeof(T).IsValueType => false,

			JsonIgnoreCondition.WhenWritingDefault when !typeof(T).IsValueType => value is null,
			JsonIgnoreCondition.WhenWritingDefault when typeof(T).IsValueType => IsDefaultValue<T>(value),

			_ => false
		};
	}

	private static ConcurrentDictionary<Type, Func<object?, bool>> IsDefaultValueCache { get; } = new ConcurrentDictionary<Type, Func<object, bool>>()!;

	private static bool IsDefaultValue(Type type, object? value)
	{
		if (!type.IsValueType)
			return value is null;

		var comparer = IsDefaultValueCache.GetOrAdd(type, static valueType => IsDefaultValueGenericMethod.MakeGenericMethod(valueType).CreateDelegate<Func<object?, bool>>());
		return comparer(value);
	}

	private static MethodInfo IsDefaultValueGenericMethod { get; } = typeof(JsonExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(m => m.Name == nameof(IsDefaultValue) && m.IsGenericMethodDefinition);

	private static bool IsDefaultValue<T>(object? value) => EqualityComparer<T>.Default.Equals(default, (T)value!);
}