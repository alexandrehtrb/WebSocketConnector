using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using static TestShared.BloodTypeExtensions;

namespace TestShared;

// Source - https://stackoverflow.com/a/76709541
// Posted by JohanP
// Retrieved 2026-03-01, License - CC BY-SA 4.0

public class BloodTypeJsonConverter : JsonConverter<BloodType>
{
    public override BloodType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ParseFromString(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, BloodType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ConvertToString(value));
    }
}

[JsonSerializable(typeof(Person))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    //UseStringEnumConverter = true,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class AppJsonSrcGenContext : JsonSerializerContext
{
    static AppJsonSrcGenContext()
    {
        // replace default context
        Default = new AppJsonSrcGenContext(CreateJsonSerializerOptions(Default));
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions(AppJsonSrcGenContext defaultContext)
    {
        //var encoderSettings = new TextEncoderSettings();
        //encoderSettings.AllowCharacters('\u002B');
        //encoderSettings.AllowRange(UnicodeRanges.All);
        //encoderSettings.AllowRange(UnicodeRanges.BasicLatin);
        //encoderSettings.AllowRange(UnicodeRanges.Latin1Supplement); // æøå etc.

        var options = new JsonSerializerOptions(defaultContext.GeneratedSerializerOptions!)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            //Encoder = JavaScriptEncoder.Create(encoderSettings)
        };

        options.Converters.Add(new BloodTypeJsonConverter());

        return options;
    }
}