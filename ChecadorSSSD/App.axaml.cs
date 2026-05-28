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
        // Connection string MySQL
        var connection = "Server=localhost;Database=subdireccion_ss_urn;User=root;Password=admin;";

        // DbContext - MySQL (Scoped)
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connection,
                ServerVersion.AutoDetect(connection),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure()
            )
            .EnableSensitiveDataLogging(), ServiceLifetime.Scoped);

        // Services (Scoped para DBContext, Singleton los demas)
        services.AddSingleton<AuthService>();
        services.AddSingleton<ImageService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ExportService>();

        // Scoped services para evitar DbContext concurrente
        services.AddScoped<PersonasService>();
        services.AddScoped<AdministradorService>();
        services.AddScoped<ChecadorService>();
        services.AddScoped<ReportesService>(_ => new ReportesService(connection));

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
