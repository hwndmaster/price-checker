namespace Genius.PriceChecker.Core.Models;

public sealed partial record ProductRef(Guid Id) : IReference<Guid, ProductRef>;
