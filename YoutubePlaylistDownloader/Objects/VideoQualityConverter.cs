namespace YoutubePlaylistDownloader.Objects;

public class VideoQualityConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(VideoQuality) || objectType == typeof(VideoQuality?);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var label = "";
        while (reader.Read())
        {
            if (reader.TokenType != JsonToken.PropertyName)
                break;

            var propertyName = (string)reader.Value;
            if (!reader.Read())
                continue;

            if (propertyName == "Label")
            {
                label = serializer.Deserialize<string>(reader);
            }
        }
        if (label != "")
        {
            return YoutubeHelpers.FromLabel(label, 30);
        }
        return YoutubeHelpers.High720;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var quality = (VideoQuality)value;

        writer.WriteStartObject();
        writer.WritePropertyName("Label");
        serializer.Serialize(writer, quality.Label);
        writer.WriteEndObject();
    }
}
