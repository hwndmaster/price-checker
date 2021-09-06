using System.Threading.Tasks;
using Genius.Atom.Infrastructure.Commands;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Services;

namespace Genius.PriceChecker.Core.CommandHandlers
{
    internal sealed class ProductEnqueueScanCommandHandler : ICommandHandler<ProductEnqueueScanCommand>
    {
        private readonly IProductPriceManager _productMng;

        public ProductEnqueueScanCommandHandler(IProductPriceManager productMng)
        {
            _productMng = productMng;
        }

        public Task ProcessAsync(ProductEnqueueScanCommand command)
        {
            _productMng.EnqueueScan(command.ProductId);

            return Task.CompletedTask;
        }
    }
}
