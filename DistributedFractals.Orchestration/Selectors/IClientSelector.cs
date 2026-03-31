namespace DistributedFractals.Orchestration.Selectors;

public interface IClientSelector
{
    Guid? Select(IReadOnlyList<Guid> workers);
}
