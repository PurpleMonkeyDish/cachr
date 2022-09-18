using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cachr.Core.Buffers;

public sealed class RentedArrayJsonConverterFactory : JsonConverterFactory
{
    private static ConcurrentDictionary<Type, JsonConverter?> s_converterCache =
        new ConcurrentDictionary<Type, JsonConverter?>();

    private Func<Type, JsonConverter?> _converterFactory = t => typeof(RentedArrayJsonConverter<>)
            .MakeGenericType(t.GetGenericArguments()[0])?
            .GetProperty("Instance")?
            .GetValue(null)
        as JsonConverter;

    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        return typeToConvert.GetGenericTypeDefinition() == typeof(RentedArray<>);
    }


    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        s_converterCache.GetOrAdd(typeToConvert, _converterFactory);
}
