namespace Genius.PriceChecker.Dto.References;

public sealed partial record ProductPriceRef(Guid Id) : IReference<Guid, ProductPriceRef>;
