using DistributedFractals.Server.Core;

namespace DistributedFractals.Server.Messages;

public abstract record Message(MessageNodeIdentifier Sender);