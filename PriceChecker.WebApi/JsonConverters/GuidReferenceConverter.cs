using System.Text.Json;
using System.Text.Json.Serialization;

namespace Genius.PriceChecker.WebApi.JsonConverters;

/// <summary>
///   Serializes Guid-keyed references as plain Guid strings.
///   The counterpart of Atom's <c>ReferenceConverter</c>, which only covers int-keyed references.
/// </summary>
public sealed class GuidReferenceConverter<TReference> : JsonConverter<TReference>
    where TReference : IReference<Guid, TReference>
{
    public override TReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return TReference.Create(reader.GetGuid());
        }

        throw new JsonException("Expected a GUID string for reference.");
    }

    public override void Write(Utf8JsonWriter writer, TReference value, JsonSerializerOptions options)
    {
        Guard.NotNull(writer);
        writer.WriteStringValue(value.Id);
    }
}
