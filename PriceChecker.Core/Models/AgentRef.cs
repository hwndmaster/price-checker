namespace Genius.PriceChecker.Core.Models;

public sealed partial record AgentRef(Guid Id) : IReference<Guid, AgentRef>;
