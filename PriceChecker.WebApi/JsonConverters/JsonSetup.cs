using System.Text.Json;
using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.WebApi.JsonConverters;

public static class JsonSetup
{
    public static void SetupJsonOptions(JsonSerializerOptions options)
    {
        Guard.NotNull(options);

        options.Converters.Add(new GuidReferenceConverter<AgentRef>());
        options.Converters.Add(new GuidReferenceConverter<ProductRef>());
        options.Converters.Add(new GuidReferenceConverter<ProductSourceRef>());
        options.Converters.Add(new GuidReferenceConverter<ProductPriceRef>());
    }
}
