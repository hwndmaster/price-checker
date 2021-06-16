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
        bool HasErrors { get; }
        bool HasNewLowestPrice { get; }
    }

    internal sealed class TrackerScanContext : ITrackerScanContext
    {
        private readonly Subject<(TrackerScanStatus status, double Progress)> _scanProgress = new();

        private bool _started;
        private int _count;
        private int _finished;

        public void NotifyStarted(int count)
        {
            _started = true;
            _count = count;
            _finished = 0;
            HasErrors = false;
            HasNewLowestPrice = false;

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

            HasErrors = HasErrors || productScanStatus == ProductScanStatus.Failed;
            HasNewLowestPrice = HasNewLowestPrice || productScanStatus == ProductScanStatus.ScannedNewLowest;

            if (isFinished)
                _finished++;

            double progress = CalculateProgress();

            TrackerScanStatus status = Helpers.TrackerScanStatus.InProgress;
            if (_finished == _count)
            {
                _started = false;
                status = Helpers.TrackerScanStatus.Finished;
            }
            else if (HasErrors)
                status = Helpers.TrackerScanStatus.InProgressWithErrors;

            _scanProgress.OnNext((status, progress));
        }

        private double CalculateProgress()
            => _finished == 0
                ? 1.0d / (_count * 2)
                : 1.0d * _finished / _count;

        public IObservable<(TrackerScanStatus Status, double Progress)> ScanProgress => _scanProgress;

        public bool HasErrors { get; private set; }
        public bool HasNewLowestPrice { get; private set; }
    }
}