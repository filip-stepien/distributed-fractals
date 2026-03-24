using DistributedFractals.Core.Core;

namespace DistributedFractals.Server.Messages;

public sealed record RenderResultMessage(
    Guid Sender,
    FractalResult Result
) : BaseMessage(Sender);
