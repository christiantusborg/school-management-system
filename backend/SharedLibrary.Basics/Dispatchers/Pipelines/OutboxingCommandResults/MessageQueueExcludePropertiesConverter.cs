using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.OutboxingCommandResults;

public class MessageQueueExcludePropertiesConverter : JsonConverter<object>
{
    /// <summary>
    ///     Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <param name="objectType"></param>
    /// <returns></returns>
    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    /// <summary>
    ///     Reads and converts the JSON to type.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return null;
    }

    /// <summary>
    ///     Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
    
        Debug.Assert(value != null, message: nameof(value) + " != null");

        foreach (var prop in value.GetType().GetProperties())
        {
            // Skip properties with the 'MessageQueuePropertyExcludeAttribute'
            if (prop.CustomAttributes.Any(x => x.AttributeType == typeof(MessageQueuePropertyExcludeAttribute)))
            {
                continue;
            }

            // Check if the property is indexed (has parameters)
            if (prop.GetIndexParameters().Length == 0)
            {
                writer.WritePropertyName(prop.Name);
            
                // Safely get the property value without index parameters
                var propValue = prop.GetValue(value, null);

                // Serialize the property value
                JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
            }
            else
            {
                // Handle indexed properties if needed, otherwise skip them
                continue;
            }
        }

        writer.WriteEndObject();
    }
}