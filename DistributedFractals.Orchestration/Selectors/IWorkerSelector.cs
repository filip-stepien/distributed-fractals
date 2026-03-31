namespace DistributedFractals.Orchestration.Selectors;

public interface IWorkerSelector
{
    Guid? Select(IReadOnlyList<Guid> workers);
}
