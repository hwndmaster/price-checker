using System.Text.Json;
using System.Text.Json.Serialization;
using Genius.Atom.Data.JsonPersistence;

namespace Genius.PriceChecker.Core.Persistence;

/// <summary>
///   Serializes Guid-keyed references as plain Guid strings, which keeps the data files
///   created before Atom introduced typed references readable.
/// </summary>
internal sealed class GuidReferenceJsonConverter<TReference> : JsonConverter<TReference>, IJsonConverter
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
