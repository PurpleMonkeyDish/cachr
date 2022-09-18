using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cachr.Core.Buffers;

public sealed class RentedArrayJsonConverter<T> : JsonConverter<RentedArray<T>>
{
    public static RentedArrayJsonConverter<T> Instance { get; } = new();

    public override RentedArray<T>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var returnValue = RentedArray<T>.Empty;
            int size = 0;
            while (reader.Read())
            {

                if (reader.TokenType == JsonTokenType.EndArray) break;
                size++;
                var previous = returnValue;
                returnValue = RentedArray<T>.FromDefaultPool(size);
                if (previous.ArraySegment.Count > 0)
                {
                    previous.ArraySegment.CopyTo(returnValue.ArraySegment);
                    previous.Dispose();
                }

                if (reader.TokenType == JsonTokenType.Null)
                {

                    returnValue.ArraySegment.Array![size-1 + returnValue.ArraySegment.Offset] = default!;
                    continue;
                }

                var nextValue = JsonSerializer.Deserialize<T>(ref reader, options);
                returnValue.ArraySegment.Array![size - 1 + returnValue.ArraySegment.Offset] = nextValue!;
            }

            return returnValue;
        }

        throw new JsonException($"Unexpected token {reader.TokenType}");
    }

    private RentedArray<T>? FromArray(T[]? array)
    {
        if(array is null) return null;
        if (array.Length == 0) return RentedArray<T>.Empty;
        var returnValue = RentedArray<T>.FromDefaultPool(array.Length);
        // RentedArray NEVER gives us an array segment with a null array.
        Array.Copy(array, 0, returnValue.ArraySegment.Array!, returnValue.ArraySegment.Offset, returnValue.ArraySegment.Count);
        return returnValue;
    }

    public override void Write(Utf8JsonWriter writer, RentedArray<T> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize<IList<T>>(writer, value.ArraySegment, options);
}
