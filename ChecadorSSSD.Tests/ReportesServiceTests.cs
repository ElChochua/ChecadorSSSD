using ChecadorSSSD.Models;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.Tests;

public class ReportesServiceTests : TestBase
{
    [Fact]
    public async Task GenerarReporte_SinRegistros_DebeRetornarListaVacia()
    {
        // Arrange
        using var context = CreateContext();
        var service = new ReportesService(context);

        // Act
        var reporte = await service.GenerarReporteAsync(
            DateTime.Now.AddDays(-30),
            DateTime.Now);

        // Assert
        Assert.Empty(reporte);
    }

    [Fact]
    public async Task GenerarReporte_ConEntradaYSalida_DebeCalcularHoras()
    {
        // Arrange
        using var context = CreateContext();
        var personasService = new PersonasService(context);
        var checadorService = new ChecadorService(context);
        var reportesService = new ReportesService(context);

        var persona = await personasService.CrearAsync(new Persona
        {
            Nombres = "Juan",
            Apellidos = "Perez",
            NumeroCuenta = "REPORT01",
            TipoUsuario = TipoUsuario.Brigadista
        });

        // Marcar entrada y salida
        await checadorService.MarcarEntradaSalidaAsync("REPORT01");
        await Task.Delay(100); // Pequeña pausa para diferenciar timestamps
        await checadorService.MarcarEntradaSalidaAsync("REPORT01");

        // Act
        var reporte = await reportesService.GenerarReporteAsync(
            DateTime.Now.AddDays(-1),
            DateTime.Now.AddDays(1));

        // Assert
        Assert.Single(reporte);
        Assert.Equal("Juan Perez", reporte[0].NombreCompleto);
        Assert.Equal(1, reporte[0].TotalEntradas);
        Assert.Equal(1, reporte[0].TotalSalidas);
        Assert.True(reporte[0].HorasAcumuladas >= 0);
    }

    [Fact]
    public async Task GenerarReporte_FiltrarPorTipoUsuario_DebeRetornarSoloEseTipo()
    {
        // Arrange
        using var context = CreateContext();
        var personasService = new PersonasService(context);
        var checadorService = new ChecadorService(context);
        var reportesService = new ReportesService(context);

        await personasService.CrearAsync(new Persona { Nombres = "Brigadista1", Apellidos = "Test", NumeroCuenta = "B001", TipoUsuario = TipoUsuario.Brigadista });
        await personasService.CrearAsync(new Persona { Nombres = "Asesor1", Apellidos = "Test", NumeroCuenta = "A001", TipoUsuario = TipoUsuario.Asesor });

        await checadorService.MarcarEntradaSalidaAsync("B001");
        await checadorService.MarcarEntradaSalidaAsync("A001");

        // Act
        var reporte = await reportesService.GenerarReporteAsync(
            DateTime.Now.AddDays(-1),
            DateTime.Now.AddDays(1),
            TipoUsuario.Brigadista);

        // Assert
        Assert.Single(reporte);
        Assert.Equal("Brigadista1 Test", reporte[0].NombreCompleto);
    }

    [Fact]
    public async Task GenerarReporte_EntradaSinSalida_NoDebeContarHoras()
    {
        // Arrange
        using var context = CreateContext();
        var personasService = new PersonasService(context);
        var checadorService = new ChecadorService(context);
        var reportesService = new ReportesService(context);

        await personasService.CrearAsync(new Persona { Nombres = "SoloEntrada", Apellidos = "Test", NumeroCuenta = "S001", TipoUsuario = TipoUsuario.Brigadista });
        await checadorService.MarcarEntradaSalidaAsync("S001");

        // Act - No marcamos salida
        var reporte = await reportesService.GenerarReporteAsync(
            DateTime.Now.AddDays(-1),
            DateTime.Now.AddDays(1));

        // Assert
        Assert.Single(reporte);
        Assert.Equal(1, reporte[0].TotalEntradas);
        Assert.Equal(0, reporte[0].TotalSalidas);
        Assert.Equal(0, reporte[0].HorasAcumuladas);
    }
}
