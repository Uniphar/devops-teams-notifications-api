namespace Teams.Cards.BotFramework.Serialization;

/// <summary>
/// When applied to a property with a value tuple type,
/// (de-)serializes the tuple as an object with properties
/// matching the names in the constructor.
/// 
/// When applied to a property of arbitrary type,
/// and one and only one property name is specified,
/// (de-)serializes the value as an object with a
/// property matching that name.
/// </summary>
/// 
/// <remarks>
/// 
/// <example>
///
///	<code>
///	public class A
///	{
///		[ObjectTupleConverter("id", "name", "status")]
///		public (Guid Id, string Name, string Status) Tenant { get; init; }
///		
///		[JsonPropertyName("user"), ObjectTupleConverter("id")]
///		public Guid UserId { get; init; }
///	}
///	</code>
/// 
///	Serializes/deserializes to/from:
///	<code>
///	{
///		"tenant": {
///			"id": "xxxx-xxxx...",
///			"name": "someName",
///			"status": "some-status"
///		},
///		"user": {
///			"id": "xxxx-xxxx..."
///		}
///	}
///	</code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
internal class ObjectTupleAttribute : JsonConverterAttribute
{
	private string[] PropertyNames { get; }

	public ObjectTupleAttribute(string prop)
	{
		PropertyNames = [prop];
	}

	public ObjectTupleAttribute(string prop1, string prop2)
	{
		PropertyNames = [prop1, prop2];
	}

	public ObjectTupleAttribute(string prop1, string prop2, string prop3)
	{
		PropertyNames = [prop1, prop2, prop3];
	}

	public ObjectTupleAttribute(string prop1, string prop2, string prop3, string prop4)
	{
		PropertyNames = [prop1, prop2, prop3, prop4];
	}

	public ObjectTupleAttribute(string prop1, string prop2, string prop3, string prop4, string prop5)
	{
		PropertyNames = [prop1, prop2, prop3, prop4, prop5];
	}

	public ObjectTupleAttribute(string prop1, string prop2, string prop3, string prop4, string prop5, string prop6)
	{
		PropertyNames = [prop1, prop2, prop3, prop4, prop5, prop6];
	}

	public ObjectTupleAttribute(string prop1, string prop2, string prop3, string prop4, string prop5, string prop6, string prop7)
	{
		PropertyNames = [prop1, prop2, prop3, prop4, prop5, prop6, prop7];
	}

	public override JsonConverter? CreateConverter(Type typeToConvert)
	{
		var isTupleType = typeToConvert.IsValueTuple();
		var tupleTypes = typeToConvert.GetGenericArguments();
		var converterType = PropertyNames switch
		{
			{ Length: 1 } when !isTupleType => typeof(ObjectTupleConverter<>).MakeGenericType(typeToConvert),
			{ Length: 1 } when isTupleType => throw new InvalidOperationException("Incompatible type for ObjectTupleConverter - when only property name is specified, the type must be a regular non-value tuple type"),

			{ Length: var propCount } when propCount != tupleTypes.Length => throw new InvalidOperationException("Incompatible type for ObjectTupleConverter - when more than one property is specified, the type must be a value tuple with the same number of type parameters"),

			{ Length: 2 } when isTupleType => typeof(ObjectTupleConverter<,>).MakeGenericType(tupleTypes),
			{ Length: 3 } when isTupleType => typeof(ObjectTupleConverter<,,>).MakeGenericType(tupleTypes),
			{ Length: 4 } when isTupleType => typeof(ObjectTupleConverter<,,,>).MakeGenericType(tupleTypes),
			{ Length: 5 } when isTupleType => typeof(ObjectTupleConverter<,,,,>).MakeGenericType(tupleTypes),
			{ Length: 6 } when isTupleType => typeof(ObjectTupleConverter<,,,,,>).MakeGenericType(tupleTypes),
			{ Length: 7 } when isTupleType => typeof(ObjectTupleConverter<,,,,,,>).MakeGenericType(tupleTypes),

			_ => throw new InvalidOperationException("Unknown error - unsupported number of property names")
		};

		return converterType.Instantiate<JsonConverter>(PropertyNames);
	}
}

file sealed class ObjectTupleConverter<T>(string prop) : JsonConverter<T>
{
	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.StartObject)
			throw new InvalidOperationException("Value is not an object");

		while (true)
		{
			reader.Read();
			if (reader.TokenType is JsonTokenType.EndObject)
				break;

			if (reader.TokenType is not JsonTokenType.PropertyName)
				throw new InvalidOperationException("Expected property name");

			if (reader.ValueTextEquals(prop))
			{
				var value = reader.ReadPropertyValue<T>(options)!;
				reader.SkipRemainingProperties();
				return value;
			}
			else
			{
				reader.SkipProperty();
			}
		}

		return default;
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyIfNotIgnored(prop, value, options);
		writer.WriteEndObject();
	}
}

file sealed class ObjectTupleConverter<T1, T2>(string prop1, string prop2) : JsonConverter<ValueTuple<T1, T2>>
{
	public override (T1, T2) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.StartObject)
			throw new InvalidOperationException("Value is not an object");

		T1 value1 = default!;
		T2 value2 = default!;

		while (true)
		{
			reader.Read();
			if (reader.TokenType is JsonTokenType.EndObject)
				break;

			if (reader.TokenType is not JsonTokenType.PropertyName)
				throw new InvalidOperationException("Expected property name");
			else if (reader.ValueTextEquals(prop1))
				value1 = reader.ReadPropertyValue<T1>(options)!;
			else if (reader.ValueTextEquals(prop2))
				value2 = reader.ReadPropertyValue<T2>(options)!;
			else
				reader.SkipProperty();
		}

		return (value1, value2)!;
	}

	public override void Write(Utf8JsonWriter writer, (T1, T2) value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyIfNotIgnored(prop1, value.Item1, options);
		writer.WritePropertyIfNotIgnored(prop2, value.Item2, options);
		writer.WriteEndObject();
	}
}

file sealed class ObjectTupleConverter<T1, T2, T3>(string prop1, string prop2, string prop3) : JsonConverter<ValueTuple<T1, T2, T3>>
{
	public override (T1, T2, T3) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.StartObject)
			throw new InvalidOperationException("Value is not an object");

		T1 value1 = default!;
		T2 value2 = default!;
		T3 value3 = default!;

		while (true)
		{
			reader.Read();
			if (reader.TokenType is JsonTokenType.EndObject)
				break;

			if (reader.TokenType is not JsonTokenType.PropertyName)
				throw new InvalidOperationException("Expected property name");
			else if (reader.ValueTextEquals(prop1))
				value1 = reader.ReadPropertyValue<T1>(options)!;
			else if (reader.ValueTextEquals(prop2))
				value2 = reader.ReadPropertyValue<T2>(options)!;
			else if (reader.ValueTextEquals(prop3))
				value3 = reader.ReadPropertyValue<T3>(options)!;
			else
				reader.SkipProperty();
		}

		return (value1, value2, value3)!;
	}

	public override void Write(Utf8JsonWriter writer, (T1, T2, T3) value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyIfNotIgnored(prop1, value.Item1, options);
		writer.WritePropertyIfNotIgnored(prop2, value.Item2, options);
		writer.WritePropertyIfNotIgnored(prop3, value.Item3, options);
		writer.WriteEndObject();
	}
}

file sealed class ObjectTupleConverter<T1, T2, T3, T4>(string prop1, string prop2, string prop3, string prop4) : JsonConverter<ValueTuple<T1, T2, T3, T4>>
{
	public override (T1, T2, T3, T4) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.StartObject)
			throw new InvalidOperationException("Value is not an object");

		T1 value1 = default!;
		T2 value2 = default!;
		T3 value3 = default!;
		T4 value4 = default!;

		while (true)
		{
			reader.Read();
			if (reader.TokenType is JsonTokenType.EndObject)
				break;

			if (reader.TokenType is not JsonTokenType.PropertyName)
				throw new InvalidOperationException("Expected property name");
			else if (reader.ValueTextEquals(prop1))
				value1 = reader.ReadPropertyValue<T1>(options)!;
			else if (reader.ValueTextEquals(prop2))
				value2 = reader.ReadPropertyValue<T2>(options)!;
			else if (reader.ValueTextEquals(prop3))
				value3 = reader.ReadPropertyValue<T3>(options)!;
			else if (reader.ValueTextEquals(prop4))
				value4 = reader.ReadPropertyValue<T4>(options)!;
			else
				reader.SkipProperty();
		}

		return (value1, value2, value3, value4)!;
	}

	public override void Write(Utf8JsonWriter writer, (T1, T2, T3, T4) value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyIfNotIgnored(prop1, value.Item1, options);
		writer.WritePropertyIfNotIgnored(prop2, value.Item2, options);
		writer.WritePropertyIfNotIgnored(prop3, value.Item3, options);
		writer.WritePropertyIfNotIgnored(prop4, value.Item4, options);
		writer.WriteEndObject();
	}
}

file sealed class ObjectTupleConverter<T1, T2, T3, T4, T5>(string prop1, string prop2, string prop3, string prop4, string prop5) : JsonConverter<ValueTuple<T1, T2, T3, T4, T5>>
{
	public override (T1, T2, T3, T4, T5) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.StartObject)
			throw new InvalidOperationException("Value is not an object");

		T1 value1 = default!;
		T2 value2 = default!;
		T3 value3 = default!;
		T4 value4 = default!;
		T5 value5 = default!;

		while (true)
		{
			reader.Read();
			if (reader.TokenType is JsonTokenType.EndObject)
				break;

			if (reader.TokenType is not JsonTokenType.PropertyName)
				throw new InvalidOperationException("Expected property name");
			else if (reader.ValueTextEquals(prop1))
				value1 = reader.ReadPropertyValue<T1>(options)!;
			else if (reader.ValueTextEquals(prop2))
				value2 = reader.ReadPropertyValue<T2>(options)!;
			else if (reader.ValueTextEquals(prop3))
				value3 = reader.ReadPropertyValue<T3>(options)!;
			else if (reader.ValueTextEquals(prop4))
				value4 = reader.ReadPropertyValue<T4>(options)!;
			else if (reader.ValueTextEquals(prop5))
				value5 = reader.ReadPropertyValue<T5>(options)!;
			else
				reader.SkipProperty();
		}

		return (value1, value2, value3, value4, value5)!;
	}

	public override void Write(Utf8JsonWriter writer, (T1, T2, T3, T4, T5) value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyIfNotIgnored(prop1, value.Item1, options);
		writer.WritePropertyIfNotIgnored(prop2, value.Item2, options);
		writer.WritePropertyIfNotIgnored(prop3, value.Item3, options);
		writer.WritePropertyIfNotIgnored(prop4, value.Item4, options);
		writer.WritePropertyIfNotIgnored(prop5, value.Item5, options);
		writer.WriteEndObject();
	}
}

file sealed class ObjectTupleConverter<T1, T2, T3, T4, T5, T6>(string prop1, string prop2, string prop3, string prop4, string prop5, string prop6) : JsonConverter<ValueTuple<T1, T2, T3, T4, T5, T6>>
{
	public override (T1, T2, T3, T4, T5, T6) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.StartObject)
			throw new InvalidOperationException("Value is not an object");

		T1 value1 = default!;
		T2 value2 = default!;
		T3 value3 = default!;
		T4 value4 = default!;
		T5 value5 = default!;
		T6 value6 = default!;

		while (true)
		{
			reader.Read();
			if (reader.TokenType is JsonTokenType.EndObject)
				break;

			if (reader.TokenType is not JsonTokenType.PropertyName)
				throw new InvalidOperationException("Expected property name");
			else if (reader.ValueTextEquals(prop1))
				value1 = reader.ReadPropertyValue<T1>(options)!;
			else if (reader.ValueTextEquals(prop2))
				value2 = reader.ReadPropertyValue<T2>(options)!;
			else if (reader.ValueTextEquals(prop3))
				value3 = reader.ReadPropertyValue<T3>(options)!;
			else if (reader.ValueTextEquals(prop4))
				value4 = reader.ReadPropertyValue<T4>(options)!;
			else if (reader.ValueTextEquals(prop5))
				value5 = reader.ReadPropertyValue<T5>(options)!;
			else if (reader.ValueTextEquals(prop6))
				value6 = reader.ReadPropertyValue<T6>(options)!;
			else
				reader.SkipProperty();
		}

		return (value1, value2, value3, value4, value5, value6)!;
	}

	public override void Write(Utf8JsonWriter writer, (T1, T2, T3, T4, T5, T6) value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyIfNotIgnored(prop1, value.Item1, options);
		writer.WritePropertyIfNotIgnored(prop2, value.Item2, options);
		writer.WritePropertyIfNotIgnored(prop3, value.Item3, options);
		writer.WritePropertyIfNotIgnored(prop4, value.Item4, options);
		writer.WritePropertyIfNotIgnored(prop5, value.Item5, options);
		writer.WritePropertyIfNotIgnored(prop6, value.Item6, options);
		writer.WriteEndObject();
	}
}

file sealed class ObjectTupleConverter<T1, T2, T3, T4, T5, T6, T7>(string prop1, string prop2, string prop3, string prop4, string prop5, string prop6, string prop7) : JsonConverter<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
{
	public override (T1, T2, T3, T4, T5, T6, T7) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.StartObject)
			throw new InvalidOperationException("Value is not an object");

		T1 value1 = default!;
		T2 value2 = default!;
		T3 value3 = default!;
		T4 value4 = default!;
		T5 value5 = default!;
		T6 value6 = default!;
		T7 value7 = default!;

		while (true)
		{
			reader.Read();
			if (reader.TokenType is JsonTokenType.EndObject)
				break;

			if (reader.TokenType is not JsonTokenType.PropertyName)
				throw new InvalidOperationException("Expected property name");
			else if (reader.ValueTextEquals(prop1))
				value1 = reader.ReadPropertyValue<T1>(options)!;
			else if (reader.ValueTextEquals(prop2))
				value2 = reader.ReadPropertyValue<T2>(options)!;
			else if (reader.ValueTextEquals(prop3))
				value3 = reader.ReadPropertyValue<T3>(options)!;
			else if (reader.ValueTextEquals(prop4))
				value4 = reader.ReadPropertyValue<T4>(options)!;
			else if (reader.ValueTextEquals(prop5))
				value5 = reader.ReadPropertyValue<T5>(options)!;
			else if (reader.ValueTextEquals(prop6))
				value6 = reader.ReadPropertyValue<T6>(options)!;
			else if (reader.ValueTextEquals(prop7))
				value7 = reader.ReadPropertyValue<T7>(options)!;
			else
				reader.SkipProperty();
		}

		return (value1, value2, value3, value4, value5, value6, value7)!;
	}

	public override void Write(Utf8JsonWriter writer, (T1, T2, T3, T4, T5, T6, T7) value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyIfNotIgnored(prop1, value.Item1, options);
		writer.WritePropertyIfNotIgnored(prop2, value.Item2, options);
		writer.WritePropertyIfNotIgnored(prop3, value.Item3, options);
		writer.WritePropertyIfNotIgnored(prop4, value.Item4, options);
		writer.WritePropertyIfNotIgnored(prop5, value.Item5, options);
		writer.WritePropertyIfNotIgnored(prop6, value.Item6, options);
		writer.WritePropertyIfNotIgnored(prop7, value.Item7, options);
		writer.WriteEndObject();
	}
}