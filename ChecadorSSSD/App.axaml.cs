using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChecadorSSSD.Data;
using ChecadorSSSD.Services;
using ChecadorSSSD.ViewModels;
using ChecadorSSSD.Views;
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ChecadorSSSD;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // DbContext - MySQL
        var connection = "Server=localhost;Database=checador_sssurn;User=chuas;Password=admin;";

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connection,
                ServerVersion.AutoDetect(connection),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure()
            )
            .EnableSensitiveDataLogging());

        // Services
        services.AddSingleton<AuthService>();
        services.AddSingleton<ImageService>();
        services.AddSingleton<ChecadorService>();
        services.AddSingleton<PersonasService>();
        services.AddSingleton<ReportesService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<AdministradorService>();

        // ViewModels
        services.AddSingleton<PersonasViewModel>();
        services.AddSingleton<ReportesViewModel>();
        services.AddSingleton<ChecadorViewModel>();
        services.AddSingleton<AdministracionViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        // Views
        services.AddSingleton<PersonasView>();
        services.AddSingleton<ReportesView>();
        services.AddSingleton<ChecadorView>();
        services.AddSingleton<AdministracionView>();
        services.AddSingleton<MainWindow>();
    }
}
