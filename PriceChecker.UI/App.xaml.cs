using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Infrastructure.Events;
using Genius.PriceChecker.Infrastructure.Logging;
using Genius.PriceChecker.UI.Helpers;
using Genius.PriceChecker.UI.ViewModels;
using Genius.PriceChecker.UI.Views;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;

namespace Genius.PriceChecker.UI
{
    [ExcludeFromCodeCoverage]
    public partial class App : Application
    {
        private TaskbarIcon _notifyIcon;

        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _notifyIcon = (TaskbarIcon) FindResource("NotifyIcon");

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            serviceCollection.AddSingleton<INotifyIconViewModel>((NotifyIconViewModel)_notifyIcon.DataContext);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            ServiceProvider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()
                .AddProvider(new EventBasedLoggerProvider(ServiceProvider.GetService<IEventBus>()));

            Core.Module.Initialize(ServiceProvider);

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            var manager = ServiceProvider.GetRequiredService<IProductPriceManager>();
            manager.Dispose();

            _notifyIcon.Dispose();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            Infrastructure.Module.Configure(services);
            Core.Module.Configure(services);

            // Framework:
            services.AddLogging();

            // Views, View models, and the View model factory
            services.AddTransient<IViewModelFactory, ViewModelFactory>();
            services.AddTransient<MainWindow>();
            services.AddTransient<IMainViewModel, MainViewModel>();
            services.AddTransient<ITrackerViewModel, TrackerViewModel>();
            services.AddTransient<ITrackerProductViewModel, TrackerProductViewModel>();
            services.AddTransient<IAgentsViewModel, AgentsViewModel>();
            services.AddTransient<IAgentViewModel, AgentViewModel>();
            services.AddTransient<ISettingsViewModel, SettingsViewModel>();
            services.AddTransient<ILogsViewModel, LogsViewModel>();

            // Services and Helpers:
            services.AddTransient<IUserInteraction, UserInteraction>();
            services.AddSingleton<ITrackerScanContext, TrackerScanContext>();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
#if !DEBUG
            MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
#endif
        }
    }
}
