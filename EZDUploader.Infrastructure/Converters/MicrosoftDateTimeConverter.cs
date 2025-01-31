using System.Text.Json;
using System.Text.Json.Serialization;

namespace EZDUploader.Infrastructure.Converters;

public class MicrosoftDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString();
        if (value == null)
            return default;

        // Format /Date(1234567890000-0000)/
        if (value.StartsWith("/Date(") && value.EndsWith(")/"))
        {
            var timeStr = value.Substring(6, value.Length - 8);
            var timeZonePos = timeStr.IndexOf('-');
            if (timeZonePos > 0)
            {
                timeStr = timeStr.Substring(0, timeZonePos);
            }
            
            if (long.TryParse(timeStr, out long timestamp))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
            }
        }
        
        return default;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var unixTime = ((DateTimeOffset)value).ToUnixTimeMilliseconds();
        writer.WriteStringValue($"/Date({unixTime}-0000)/");
    }
}