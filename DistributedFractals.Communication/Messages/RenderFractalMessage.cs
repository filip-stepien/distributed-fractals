using DistributedFractals.Core.Core;

namespace DistributedFractals.Server.Messages;

public sealed record RenderFractalMessage(
    Guid Sender,
    FractalGeneratorType GeneratorType,
    FractalColorizerType ColorizerType,
    IFractalGeneratorOptions Options
) : BaseMessage(Sender);
