namespace DistributedFractals.Server.Serialization;

using SystemJson = System.Text.Json;

public class JsonSerializer : ISerializer
{
    private static readonly SystemJson.JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReadOnlyMemory<byte> Serialize<T>(T data)
    {
        return SystemJson.JsonSerializer.SerializeToUtf8Bytes(data, DefaultOptions);
    }

    public T Deserialize<T>(ReadOnlyMemory<byte> bytes)
    {
        T? result = SystemJson.JsonSerializer.Deserialize<T>(bytes.Span, DefaultOptions);

        if (result is null)
        {
            throw new InvalidOperationException($"Failed to deserialize data to type {typeof(T).Name}.");
        }

        return result;
    }
}