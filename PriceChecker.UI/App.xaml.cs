using System;
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
    public partial class App : Application
    {
        private TaskbarIcon _notifyIcon;

        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            ServiceProvider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()
                .AddProvider(new EventBasedLoggerProvider(ServiceProvider.GetService<IEventBus>()));

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            _notifyIcon = (TaskbarIcon) FindResource("NotifyIcon");
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
            services.AddTransient<MainViewModel>();
            services.AddTransient<TrackerViewModel>();
            services.AddTransient<TrackerProductViewModel>();
            services.AddTransient<AgentsViewModel>();
            services.AddTransient<AgentViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<LogsViewModel>();

            // Services and Helpers:
            services.AddTransient<IUserInteraction, UserInteraction>();
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
