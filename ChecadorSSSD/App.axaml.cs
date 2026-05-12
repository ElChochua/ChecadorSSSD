using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                "Server=localhost;Database=checador_curn;User=chuas;Password=admin;",
                new MySqlServerVersion(new Version(8, 0, 2))
            ));

        // Services
        services.AddSingleton<ChecadorService>();
        services.AddSingleton<PersonasService>();
        services.AddSingleton<ReportesService>();

        // ViewModels
        services.AddSingleton<PersonasViewModel>();
        services.AddSingleton<ReportesViewModel>();
        services.AddSingleton<ChecadorViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        // Views
        services.AddSingleton<PersonasView>();
        services.AddSingleton<ReportesView>();
        services.AddSingleton<ChecadorView>();
        services.AddSingleton<MainWindow>();
    }
}
