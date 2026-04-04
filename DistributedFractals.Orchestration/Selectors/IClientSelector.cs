using DistributedFractals.Server.Core;

namespace DistributedFractals.Orchestration.Selectors;

public interface IClientSelector
{
    ClientIdentifier? Select(IReadOnlyList<ClientIdentifier> clients);
}
