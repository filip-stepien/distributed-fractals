using Avalonia.Threading;
using DistributedFractals.Logging;

namespace DistributedFractals.Gui.Services;

internal sealed class GuiLogger(Action<string> writeLine) : ILogger
{
    public void Log(string message)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            writeLine(message);
            return;
        }

        Dispatcher.UIThread.Post(() => writeLine(message));
    }
}
