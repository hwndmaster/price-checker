global using Genius.Atom.Infrastructure;

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.UI.Helpers;
using Genius.PriceChecker.UI.ViewModels;
using Genius.PriceChecker.UI.Views;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;

namespace Genius.PriceChecker.UI;

[ExcludeFromCodeCoverage]
public partial class App : Application
{
#pragma warning disable CS8618 // These fields are being initialized in OnStartup() method.
    private TaskbarIcon _notifyIcon;
    public static IServiceProvider ServiceProvider { get; private set; }
#pragma warning restore CS8618

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _notifyIcon = (TaskbarIcon) FindResource("NotifyIcon");

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        serviceCollection.AddSingleton<INotifyIconViewModel>((NotifyIconViewModel)_notifyIcon.DataContext);

        ServiceProvider = serviceCollection.BuildServiceProvider();
        Core.Module.Initialize(ServiceProvider);
        Atom.UI.Forms.Module.Initialize(ServiceProvider);

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

    private static void ConfigureServices(IServiceCollection services)
    {
        Atom.Data.Module.Configure(services);
        Atom.Infrastructure.Module.Configure(services);
        Atom.UI.Forms.Module.Configure(services);
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
        services.AddTransient<IProductInteraction, ProductInteraction>();
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
