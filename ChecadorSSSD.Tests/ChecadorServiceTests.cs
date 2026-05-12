using ChecadorSSSD.Models;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.Tests;

public class ChecadorServiceTests : TestBase
{
    [Fact]
    public async Task MarcarEntradaSalida_PrimeraVez_DebeMarcarEntrada()
    {
        // Arrange
        using var context = CreateContext();
        var personasService = new PersonasService(context);
        var checadorService = new ChecadorService(context);

        var persona = await personasService.CrearAsync(new Persona
        {
            Nombres = "Test",
            Apellidos = "User",
            NumeroCuenta = "TEST01",
            TipoUsuario = TipoUsuario.Brigadista
        });

        // Act
        var (accion, mensaje) = await checadorService.MarcarEntradaSalidaAsync("TEST01");

        // Assert
        Assert.Equal(TipoAccion.Entrada, accion);
        Assert.Contains("Entrada", mensaje);
    }

    [Fact]
    public async Task MarcarEntradaSalida_SegundaVez_DebeMarcarSalida()
    {
        // Arrange
        using var context = CreateContext();
        var personasService = new PersonasService(context);
        var checadorService = new ChecadorService(context);

        var persona = await personasService.CrearAsync(new Persona
        {
            Nombres = "Test",
            Apellidos = "User",
            NumeroCuenta = "TEST02",
            TipoUsuario = TipoUsuario.Brigadista
        });

        // Primera marca - Entrada
        await checadorService.MarcarEntradaSalidaAsync("TEST02");

        // Act - Segunda marca (debe ser Salida)
        var (accion, mensaje) = await checadorService.MarcarEntradaSalidaAsync("TEST02");

        // Assert
        Assert.Equal(TipoAccion.Salida, accion);
        Assert.Contains("Salida", mensaje);
    }

    [Fact]
    public async Task MarcarEntradaSalida_MatriculaInexistente_DebeRetornarError()
    {
        // Arrange
        using var context = CreateContext();
        var checadorService = new ChecadorService(context);

        // Act
        var (accion, mensaje) = await checadorService.MarcarEntradaSalidaAsync("NOEXISTE");

        // Assert
        Assert.Contains("no encontrada", mensaje);
    }

    [Fact]
    public async Task MarcarEntradaSalida_TresVeces_DebeAlternarEntradaSalida()
    {
        // Arrange
        using var context = CreateContext();
        var personasService = new PersonasService(context);
        var checadorService = new ChecadorService(context);

        await personasService.CrearAsync(new Persona
        {
            Nombres = "Test",
            Apellidos = "User",
            NumeroCuenta = "TEST03",
            TipoUsuario = TipoUsuario.Brigadista
        });

        // Act & Assert
        var (accion1, _) = await checadorService.MarcarEntradaSalidaAsync("TEST03");
        Assert.Equal(TipoAccion.Entrada, accion1);

        var (accion2, _) = await checadorService.MarcarEntradaSalidaAsync("TEST03");
        Assert.Equal(TipoAccion.Salida, accion2);

        var (accion3, _) = await checadorService.MarcarEntradaSalidaAsync("TEST03");
        Assert.Equal(TipoAccion.Entrada, accion3);
    }
}
