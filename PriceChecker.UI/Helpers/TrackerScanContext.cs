using System;
using System.Reactive.Subjects;
using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.UI.Helpers
{
    public interface ITrackerScanContext
    {
        void NotifyStarted(int count);
        void NotifyProgressChange(ProductScanStatus productScanStatus);

        IObservable<(TrackerScanStatus Status, double Progress)> ScanProgress { get; }
    }

    internal sealed class TrackerScanContext : ITrackerScanContext
    {
        private readonly Subject<(TrackerScanStatus status, double Progress)> _scanProgress = new();

        private bool _started;
        private int _count;
        private int _finished;
        private bool _hasErrors;

        public void NotifyStarted(int count)
        {
            _started = true;
            _count = count;
            _finished = 0;
            _hasErrors = false;

            var initialProgress = CalculateProgress();
            _scanProgress.OnNext((TrackerScanStatus.InProgress, initialProgress));
        }

        public void NotifyProgressChange(ProductScanStatus productScanStatus)
        {
            if (!_started)
                return;

            var isFinished = productScanStatus == ProductScanStatus.ScannedOk
                || productScanStatus == ProductScanStatus.ScannedNewLowest
                || productScanStatus == ProductScanStatus.Failed;

            _hasErrors = _hasErrors || productScanStatus == ProductScanStatus.Failed;

            if (isFinished)
                _finished++;

            double progress = CalculateProgress();

            TrackerScanStatus status = Helpers.TrackerScanStatus.InProgress;
            if (_finished == _count)
            {
                _started = false;
                status = Helpers.TrackerScanStatus.Finished;
            }
            else if (_hasErrors)
                status = Helpers.TrackerScanStatus.InProgressWithErrors;

            _scanProgress.OnNext((status, progress));
        }

        private double CalculateProgress()
            => _finished == 0
                ? 1.0d / (_count * 2)
                : 1.0d * _finished / _count;

        public IObservable<(TrackerScanStatus Status, double Progress)> ScanProgress => _scanProgress;
    }
}