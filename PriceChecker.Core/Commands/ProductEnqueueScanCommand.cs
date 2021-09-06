using System;
using Genius.Atom.Infrastructure.Commands;

namespace Genius.PriceChecker.Core.Commands
{
    public sealed class ProductEnqueueScanCommand : ICommandMessage
    {
        public ProductEnqueueScanCommand(Guid productId)
        {
            ProductId = productId;
        }

        public Guid ProductId { get; }
    }
}
