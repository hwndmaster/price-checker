using System.Diagnostics;
using System.Windows;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;

namespace Genius.PriceChecker.UI.Helpers
{
    public interface IUserInteraction
    {
        /// <summary>
        ///     Shows a message box to a user with buttons Yes and No.
        /// </summary>
        /// <param name="message">A message to show.</param>
        /// <param name="title">A title of the message box.</param>
        /// <returns>Returns true if user has selected YES. Otherwise returns false.</returns>
        bool AskForConfirmation(string message, string title);

        /// <summary>
        ///     Shows an information popup message to a user.
        /// </summary>
        /// <param name="message">A message content.</param>
        void ShowInformation(string message);

        /// <summary>
        ///     Shows a warning popup message to a user.
        /// </summary>
        /// <param name="message">A message content.</param>
        void ShowWarning(string message);

        void ShowProductInBrowser(ProductSource productSource);
    }

    public class UserInteraction : IUserInteraction
    {
        private readonly IAgentRepository _agentRepo;

        public UserInteraction(IAgentRepository agentRepo)
        {
            _agentRepo = agentRepo;
        }

        public bool AskForConfirmation(string message, string title)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo);
            return result == MessageBoxResult.Yes;
        }

        public void ShowInformation(string message)
        {
            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowWarning(string message)
        {
            MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowProductInBrowser(ProductSource productSource)
        {
            if (productSource == null)
                return;

            var agentUrl = _agentRepo.FindById(productSource.AgentId).Url;
            var url = string.Format(agentUrl, productSource.AgentArgument);

            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
}
