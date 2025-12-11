using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using GuiBuddy.Core.Repositories;
using GuiBuddy.Core.Services;
using GuiBuddy.Infrastructure.Repositories;
using GuiBuddy.Services;
using GuiBuddy.App.ViewModels;

namespace GuiBuddy.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core & Infrastructure
        services.AddSingleton<IWindowRepository, AutomationWindowRepository>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IUIMapService, UIMapService>();

        // App
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }
}

