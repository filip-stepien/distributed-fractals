namespace DistributedFractals.Server.Serialization;

public interface ISerializer
{
    ReadOnlyMemory<byte> Serialize<T>(T data);
    T Deserialize<T>(ReadOnlyMemory<byte> bytes);
}