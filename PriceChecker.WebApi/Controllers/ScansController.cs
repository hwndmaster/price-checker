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

    // Both scans hand the work to RunAndForget and answer 202, so the status has to be declared:
    // an undeclared one is documented as 200, and the generated client rejects every status it was not
    // told about — which turned an accepted scan into "An unexpected server error occurred." in the UI
    // while the scan itself ran on happily.
    [HttpPost("all")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult ScanAll()
    {
        _scanOrchestrator.ScanAsync(trigger: ScanTrigger.Manual).RunAndForget();
        return Accepted();
    }

    [HttpPost("product/{productId}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult ScanProduct([FromRoute] Guid productId)
    {
        _scanOrchestrator.ScanAsync([productId], ScanTrigger.Manual).RunAndForget();
        return Accepted();
    }

    [HttpGet("progress")]
    public ScanProgressDto GetProgress()
        => _scanContext.GetProgress();
}
