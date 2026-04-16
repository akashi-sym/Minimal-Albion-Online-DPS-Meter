using AlbionDpsMeter.Services;
using AlbionDpsMeter.ViewModels;
using AlbionDpsMeter.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using System;
using System.Threading.Tasks;

namespace AlbionDpsMeter;

public class App : Application
{
    private Window? _window;
    public IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    static void Main(string[] args)
    {
        global::WinRT.ComWrappersSupport.InitializeComWrappers();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .WriteTo.File("logs/albion-dps-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Albion DPS Meter starting");

            Application.Start(p =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                var app = new App();
            });
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application crashed");
            System.IO.File.WriteAllText("crash.log", ex.ToString());
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public App()
    {
        UnhandledException += (sender, e) =>
        {
            Log.Fatal(e.Exception, "Unhandled WinUI exception");
            System.IO.File.WriteAllText("crash.log", e.Exception.ToString());
            e.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Log.Information("OnLaunched starting");

            RegisterThemeBrushes();

            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            var services = new ServiceCollection();
            services.AddSingleton(dispatcherQueue);
            services.AddSingleton<TrackingController>();
            services.AddSingleton<ItemController>();
            services.AddSingleton<ImageController>();
            services.AddSingleton<DamageMeterViewModel>();

            Services = services.BuildServiceProvider();
            Log.Information("Services built");

            // Load item data in background (non-blocking)
            var itemController = Services.GetRequiredService<ItemController>();
            _ = Task.Run(async () =>
            {
                await itemController.LoadItemsAsync();
            });

            var viewModel = Services.GetRequiredService<DamageMeterViewModel>();
            Log.Information("ViewModel resolved");

            var imageController = Services.GetRequiredService<ImageController>();
            _window = new MainWindow(viewModel, itemController, imageController);
            Log.Information("MainWindow created");

            _window.Activate();
            Log.Information("Window activated");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "OnLaunched failed");
            System.IO.File.WriteAllText("crash.log", ex.ToString());
        }
    }

    private void RegisterThemeBrushes()
    {
        // Dark-theme brushes matching WinUI 3 defaults
        Resources["CardBackgroundFillColorDefaultBrush"] =
            new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(0x12, 0xFF, 0xFF, 0xFF));

        Resources["CardBackgroundFillColorSecondaryBrush"] =
            new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(0x08, 0xFF, 0xFF, 0xFF));

        Resources["ControlFillColorDisabledBrush"] =
            new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(0x0B, 0xFF, 0xFF, 0xFF));

        // AccentButtonStyle: blue accent background button
        var accentStyle = new Style(typeof(Microsoft.UI.Xaml.Controls.Button));
        accentStyle.Setters.Add(new Setter(
            Microsoft.UI.Xaml.Controls.Control.BackgroundProperty,
            new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x60, 0xCD, 0xFF))));
        accentStyle.Setters.Add(new Setter(
            Microsoft.UI.Xaml.Controls.Control.ForegroundProperty,
            new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x00, 0x00, 0x00))));
        Resources["AccentButtonStyle"] = accentStyle;

        Log.Information("Theme brushes registered");
    }
}
