using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using Avalonia;

namespace ChecadorSSSD
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            VerificarDpHost();
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .LogToTrace();

        private static void VerificarDpHost()
        {
            try
            {
                var servicios = ServiceController.GetServices();
                var dpHost = servicios.FirstOrDefault(s =>
                    string.Equals(s.ServiceName, "DpHost", StringComparison.OrdinalIgnoreCase));

                if (dpHost == null)
                {
                    Trace.WriteLine("[DpHost] Servicio DpHost NO encontrado en esta PC. La app no podra detectar el lector sin el runtime DigitalPersona.");
                    return;
                }

                Trace.WriteLine($"[DpHost] Servicio encontrado. Estado: {dpHost.Status}");
                if (dpHost.Status != ServiceControllerStatus.Running)
                    Trace.WriteLine("[DpHost] ADVERTENCIA: DpHost existe pero NO esta corriendo. El lector puede no detectarse.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[DpHost] No se pudo verificar el servicio: {ex.Message}");
            }
        }
    }
}
