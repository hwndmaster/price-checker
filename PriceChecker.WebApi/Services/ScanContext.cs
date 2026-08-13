using Genius.PriceChecker.Dto;

namespace Genius.PriceChecker.WebApi.Services;

public interface IScanContext
{
    void NotifyScanStarted(int productCount);
    void NotifyProductFinished(bool hasErrors, bool hasNewLowestPrice);
    ScanProgressDto GetProgress();
}

internal sealed class ScanContext : IScanContext
{
    private readonly Lock _lock = new();
    private int _total;
    private int _finished;
    private bool _hasErrors;
    private bool _hasNewLowestPrice;

    public void NotifyScanStarted(int productCount)
    {
        lock (_lock)
        {
            if (_finished >= _total)
            {
                _total = 0;
                _finished = 0;
                _hasErrors = false;
                _hasNewLowestPrice = false;
            }

            _total += productCount;
        }
    }

    public void NotifyProductFinished(bool hasErrors, bool hasNewLowestPrice)
    {
        lock (_lock)
        {
            _finished++;
            _hasErrors |= hasErrors;
            _hasNewLowestPrice |= hasNewLowestPrice;
        }
    }

    public ScanProgressDto GetProgress()
    {
        lock (_lock)
        {
            return new ScanProgressDto(_finished, _total, _hasErrors, _hasNewLowestPrice,
                IsFinished: _finished >= _total);
        }
    }
}
