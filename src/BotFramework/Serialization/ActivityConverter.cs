namespace Teams.Cards.BotFramework.Serialization;

// Ok, so why this and not [JsonPolymorphic]/[JsonDerivedType]?
// S.T.J polymorphism support can only support a single property
// We need to differentiate based on potentially multiple properties
internal sealed class ActivityConverter : JsonConverter<Activity>
{
	public override Activity? Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions options)
	{
		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;

		if (root.GetStringPropertyOrDefault("channelId") is not "msteams")
			return null;

		var type = root.GetStringPropertyOrDefault("type");
		var name = root.GetStringPropertyOrDefault("name");
		var action = root.GetStringPropertyOrDefault("action");

		return type switch
		{
			"installationUpdate" when action is "add" => doc.Deserialize<InstallationAddedActivity>(options),
			"installationUpdate" when action is "remove" => doc.Deserialize<InstallationRemovedActivity>(options),
			"message" => doc.Deserialize<MessageActivity>(options),
			_ => null
		};
	}

	public override void Write(Utf8JsonWriter writer, Activity value, JsonSerializerOptions options)
	{
		JsonSerializer.Serialize(value, options.GetTypeInfo(value.GetType()));
	}
}