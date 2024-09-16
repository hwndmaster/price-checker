global using System.Windows;
global using Genius.Atom.Infrastructure;
global using Genius.Atom.Infrastructure.Attributes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.UI.Helpers;
using Genius.PriceChecker.UI.Views;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.UI;

[ExcludeFromCodeCoverage]
public partial class App : Application
{
#pragma warning disable CS8618 // These fields are being initialized in OnStartup() method.
    private TaskbarIcon _notifyIcon;
    public static IServiceProvider ServiceProvider { get; private set; }
#pragma warning restore CS8618

    [Dangerous("Shouldn't be used from anywhere, except from unit tests of non-injectable classes.")]
    internal static void OverrideServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();
        Atom.Data.Module.Initialize(ServiceProvider);
        Atom.Infrastructure.Module.Initialize(ServiceProvider);
        PriceChecker.Core.Module.Initialize(ServiceProvider);
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

    private void ConfigureServices(IServiceCollection services)
    {
        Atom.Data.Module.Configure(services);
        Atom.Infrastructure.Module.Configure(services);
        var configuration = Atom.UI.Forms.Module.Configure(services, this);
        Core.Module.Configure(services);

        // Views, View models, View model factories
        services.AddSingleton<MainWindow>();
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddTransient<IMainViewModel, MainViewModel>();
        services.AddTransient<ITrackerViewModel, TrackerViewModel>();
        services.AddTransient<ITrackerProductViewModel, TrackerProductViewModel>();
        services.AddTransient<IAgentsViewModel, AgentsViewModel>();
        services.AddTransient<IAgentViewModel, AgentViewModel>();
        services.AddTransient<ISettingsViewModel, SettingsViewModel>();

        // AutoGrid builders
        // TODO: ...

        // Services and Helpers:
        services.AddSingleton<INotifyIconViewModel>((NotifyIconViewModel)_notifyIcon.DataContext);
        services.AddTransient<IProductInteraction, ProductInteraction>();
        services.AddSingleton<ITrackerScanContext, TrackerScanContext>();
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var logger = ServiceProvider.GetService<ILogger<App>>();
            logger?.LogCritical(e.Exception, e.Exception.Message);
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }

#if !DEBUG
        MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
#endif
    }
}
