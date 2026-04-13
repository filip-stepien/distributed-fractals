namespace DistributedFractals.Logging;

public sealed class NullLogger : ILogger
{
    public static readonly NullLogger Instance = new();

    private NullLogger() { }

    public void Log(string message) { }
}
