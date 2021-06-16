using Hardcodet.Wpf.TaskbarNotification;

namespace Genius.PriceChecker.UI.Helpers
{
    public class ShowBalloonTipEventArgs
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public BalloonIcon Icon { get; set; }
    }
}
