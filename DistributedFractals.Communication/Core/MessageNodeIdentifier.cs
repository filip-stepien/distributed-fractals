namespace DistributedFractals.Server.Core;

public sealed record MessageNodeIdentifier
{
    public string Id { get; }
    public string DisplayName { get; set; } = string.Empty;

    public MessageNodeIdentifier()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    private MessageNodeIdentifier(string id)
    {
        Id = id;
    }

    public static MessageNodeIdentifier From(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Identifier cannot be null or empty.", nameof(id));
        }

        return new MessageNodeIdentifier(id);
    }

    public override string ToString() => Id;
}