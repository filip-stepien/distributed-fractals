namespace DistributedFractals.Logging;

public sealed class CompositeLogger(params ILogger[] loggers) : ILogger
{
    public void Log(string message)
    {
        foreach (ILogger logger in loggers)
            logger.Log(message);
    }
}
