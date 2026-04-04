namespace DistributedFractals.Server.Core;

public sealed record ClientIdentifier(Guid Id, string DisplayName)
{
    public bool Equals(ClientIdentifier? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}
