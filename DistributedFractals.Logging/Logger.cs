namespace DistributedFractals.Logging;

public static class Logger
{
    public static ILogger Instance { get; private set; } = NullLogger.Instance;

    public static void Initialize(ILogger logger)
    {
        Instance = logger;
    }

    public static void Log(string message) => Instance.Log(message);
}
