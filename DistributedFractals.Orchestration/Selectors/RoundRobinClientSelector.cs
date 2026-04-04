using DistributedFractals.Server.Core;

namespace DistributedFractals.Orchestration.Selectors;

public sealed class RoundRobinClientSelector : IClientSelector
{
    private int _index;

    public ClientIdentifier? Select(IReadOnlyList<ClientIdentifier> clients)
    {
        if (clients.Count == 0)
        {
            return null;
        }

        ClientIdentifier selected = clients[_index % clients.Count];
        _index = (_index + 1) % clients.Count;
        return selected;
    }
}
