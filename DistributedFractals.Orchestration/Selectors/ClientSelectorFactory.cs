namespace DistributedFractals.Orchestration.Selectors;

public static class ClientSelectorFactory
{
    public static IClientSelector Create(ClientSelectorType type) => type switch
    {
        ClientSelectorType.RoundRobin => new RoundRobinClientSelector(),
        _ => throw new NotSupportedException($"Unsupported selector type: {type}")
    };
}
