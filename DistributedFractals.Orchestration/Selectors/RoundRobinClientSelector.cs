namespace DistributedFractals.Orchestration.Selectors;

public sealed class RoundRobinClientSelector : IClientSelector
{
    private int _index;

    public Guid? Select(IReadOnlyList<Guid> workers)
    {
        if (workers.Count == 0) return null;
        var selected = workers[_index % workers.Count];
        _index = (_index + 1) % workers.Count;
        return selected;
    }
}
