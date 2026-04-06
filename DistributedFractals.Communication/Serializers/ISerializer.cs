namespace DistributedFractals.Server.Serializers;

public interface ISerializer
{
    ReadOnlyMemory<byte> Serialize<T>(T data);
    T Deserialize<T>(ReadOnlyMemory<byte> bytes);
}