using ChecadorSSSD.Models;
using ChecadorSSSD.Services;

namespace ChecadorSSSD.Tests;

public class PersonasServiceTests : TestBase
{
    [Fact]
    public async Task CrearPersona_DebeAgregarPersonaALaBaseDeDatos()
    {
        // Arrange
        using var context = CreateContext();
        var service = new PersonasService(context);
        var persona = new Persona
        {
            Nombres = "Juan",
            Apellidos = "Perez",
            NumeroCuenta = "12345",
            TipoUsuario = TipoUsuario.Brigadista
        };

        // Act
        var resultado = await service.CrearAsync(persona);

        // Assert
        Assert.NotNull(resultado);
        Assert.True(resultado.Id > 0);
        Assert.Equal("Juan", resultado.Nombres);
    }

    [Fact]
    public async Task ObtenerTodas_DebeRetornarTodasLasPersonas()
    {
        // Arrange
        using var context = CreateContext();
        var service = new PersonasService(context);
        await service.CrearAsync(new Persona { Nombres = "Ana", Apellidos = "Garcia", NumeroCuenta = "111", TipoUsuario = TipoUsuario.Asesor });
        await service.CrearAsync(new Persona { Nombres = "Luis", Apellidos = "Martinez", NumeroCuenta = "222", TipoUsuario = TipoUsuario.Brigadista });

        // Act
        var personas = await service.ObtenerTodasAsync();

        // Assert
        Assert.Equal(2, personas.Count);
    }

    [Fact]
    public async Task ActualizarPersona_DebeModificarDatos()
    {
        // Arrange
        using var context = CreateContext();
        var service = new PersonasService(context);
        var persona = await service.CrearAsync(new Persona { Nombres = "Carlos", Apellidos = "Lopez", NumeroCuenta = "333", TipoUsuario = TipoUsuario.PersonalAdministrativo });

        // Act
        persona.Nombres = "Carlos Updated";
        var resultado = await service.ActualizarAsync(persona);

        // Assert
        Assert.Equal("Carlos Updated", resultado.Nombres);
    }

    [Fact]
    public async Task EliminarPersona_DebeEliminarDeLaBaseDeDatos()
    {
        // Arrange
        using var context = CreateContext();
        var service = new PersonasService(context);
        var persona = await service.CrearAsync(new Persona { Nombres = "Maria", Apellidos = "Rodriguez", NumeroCuenta = "444", TipoUsuario = TipoUsuario.Brigadista });

        // Act
        await service.EliminarAsync(persona.Id);
        var personas = await service.ObtenerTodasAsync();

        // Assert
        Assert.Empty(personas);
    }
}
