namespace Genius.PriceChecker.Dto.References;

public sealed partial record ProductRef(Guid Id) : IReference<Guid, ProductRef>;
