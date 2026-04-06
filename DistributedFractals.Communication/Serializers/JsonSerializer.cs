using System.Text;
using Newtonsoft.Json;

namespace DistributedFractals.Server.Serializers;

public class JsonSerializer : ISerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    public ReadOnlyMemory<byte> Serialize<T>(T data)
    {
        string json = JsonConvert.SerializeObject(data, Settings);
        return Encoding.UTF8.GetBytes(json);
    }

    public T Deserialize<T>(ReadOnlyMemory<byte> bytes)
    {
        string json = Encoding.UTF8.GetString(bytes.Span);
        T? result = JsonConvert.DeserializeObject<T>(json, Settings);

        return result ?? throw new InvalidOperationException($"Failed to deserialize data to type {typeof(T).Name}.");
    }
}