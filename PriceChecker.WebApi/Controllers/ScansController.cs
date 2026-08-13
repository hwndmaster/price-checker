using Genius.Atom.Infrastructure.Tasks;
using Genius.Atom.Web.Controllers;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Genius.PriceChecker.WebApi.Controllers;

public sealed class ScansController : BaseController
{
    private readonly IScanOrchestrator _scanOrchestrator;
    private readonly IScanContext _scanContext;

    public ScansController(IScanOrchestrator scanOrchestrator, IScanContext scanContext)
    {
        _scanOrchestrator = scanOrchestrator.NotNull();
        _scanContext = scanContext.NotNull();
    }

    [HttpPost("all")]
    public IActionResult ScanAll()
    {
        _scanOrchestrator.ScanAsync().RunAndForget();
        return Accepted();
    }

    [HttpPost("product/{productId}")]
    public IActionResult ScanProduct([FromRoute] Guid productId)
    {
        _scanOrchestrator.ScanAsync([productId]).RunAndForget();
        return Accepted();
    }

    [HttpGet("progress")]
    public ScanProgressDto GetProgress()
        => _scanContext.GetProgress();
}
