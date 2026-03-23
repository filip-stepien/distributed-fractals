using System.Buffers.Binary;

namespace DistributedFractals.Server.Tcp;

internal sealed class TcpStream(Stream stream)
{
    private const int LengthPrefixSize = 4;
    
    private async Task ReadExactlyAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        int totalRead = 0;

        while (totalRead < buffer.Length)
        {
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);

            if (bytesRead == 0)
            {
                throw new EndOfStreamException("Connection closed before full baseMessage was received.");
            }

            totalRead += bytesRead;
        }
    }
    
    public async Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        byte[] lengthPrefix = new byte[LengthPrefixSize];
        BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, data.Length);

        await stream.WriteAsync(lengthPrefix, cancellationToken);
        await stream.WriteAsync(data, cancellationToken);
    }

    public async Task<Memory<byte>> ReadAsync(CancellationToken cancellationToken = default)
    {
        byte[] lengthPrefix = new byte[LengthPrefixSize];
        await ReadExactlyAsync(lengthPrefix, cancellationToken);

        int messageLength = BinaryPrimitives.ReadInt32BigEndian(lengthPrefix);

        byte[] buffer = new byte[messageLength];
        await ReadExactlyAsync(buffer, cancellationToken);

        return buffer;
    }
}
