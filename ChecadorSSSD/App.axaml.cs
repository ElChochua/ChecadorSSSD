using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChecadorSSSD.Data;
using ChecadorSSSD.Services;
using ChecadorSSSD.ViewModels;
using ChecadorSSSD.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
            mainWindow.Opened += async (_, _) =>
                await VerifyDatabaseConnectionAsync(desktop, mainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Connection string MySQL - lee de appconfig.json (key "MySqlConnection"); fallback al valor por defecto.
        var settingsSvc = new SettingsService();
        var databaseConnection = new DatabaseConnectionService(settingsSvc);
        var connection = databaseConnection.GetRuntimeConnectionString();

        // DbContext - MySQL (Scoped)
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connection,
                new MySqlServerVersion(new Version(8, 0, 0)),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure()
            )
            .EnableSensitiveDataLogging(), ServiceLifetime.Scoped);

        // Services (Scoped para DBContext, Singleton los demas)
        services.AddSingleton<AuthService>();
        services.AddSingleton<ImageService>();
        services.AddSingleton(settingsSvc);
        services.AddSingleton(databaseConnection);
        services.AddSingleton<ExportService>();

        // Scoped services para evitar DbContext concurrente
        services.AddScoped<PersonasService>();
        services.AddScoped<AdministradorService>();
        services.AddScoped<ChecadorService>();
        services.AddScoped<ReportesService>(_ => new ReportesService(connection));

        // Fingerprint service
        services.AddSingleton<FingerprintService>();

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

    private async Task VerifyDatabaseConnectionAsync(
        IClassicDesktopStyleApplicationLifetime desktop,
        MainWindow mainWindow)
    {
        try
        {
            if (Services == null)
                return;

            var connectionService = Services.GetRequiredService<DatabaseConnectionService>();
            var result = await connectionService.TestCurrentAsync();
            if (result.Success)
                return;

            FingerprintLogger.LogDb($"Conexión inicial a MySQL fallida: {result.Message}");

            var dialog = new DatabaseConnectionDialog(connectionService, result.Message);
            var shouldRestart = await dialog.ShowDialog<bool>(mainWindow);
            if (!shouldRestart)
                return;

            var executable = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executable))
                throw new InvalidOperationException("No se encontró la ruta del ejecutable.");

            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = true
            });
            desktop.Shutdown();
        }
        catch (Exception ex)
        {
            FingerprintLogger.LogError($"Error al verificar o configurar MySQL: {ex}");
            mainWindow.Title = "Checador SSSD — sin conexión a MySQL";
        }
    }
}
