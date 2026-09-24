using System;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace ChecadorSSSD.Services;

public sealed class DatabaseConnectionService
{
    public const string DefaultConnectionString =
        "Server=localhost;Database=subdireccion_ss_urn;User=root;Password=admin;";

    private readonly SettingsService _settings;

    public DatabaseConnectionService(SettingsService settings)
    {
        _settings = settings;
    }

    public string CurrentConnectionString =>
        _settings.Get("MySqlConnection", DefaultConnectionString);

    public string GetRuntimeConnectionString()
    {
        try
        {
            return new MySqlConnectionStringBuilder(CurrentConnectionString).ConnectionString;
        }
        catch
        {
            return DefaultConnectionString;
        }
    }

    public DatabaseConnectionSettings GetCurrentSettings()
    {
        try
        {
            var builder = new MySqlConnectionStringBuilder(CurrentConnectionString);
            return new DatabaseConnectionSettings(
                builder.Server,
                builder.Port,
                builder.Database,
                builder.UserID,
                builder.Password);
        }
        catch
        {
            var defaults = new MySqlConnectionStringBuilder(DefaultConnectionString);
            return new DatabaseConnectionSettings(
                defaults.Server,
                defaults.Port,
                defaults.Database,
                defaults.UserID,
                defaults.Password);
        }
    }

    public static string BuildConnectionString(DatabaseConnectionSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Server))
            throw new ArgumentException("El servidor es obligatorio.");
        if (string.IsNullOrWhiteSpace(settings.Database))
            throw new ArgumentException("La base de datos es obligatoria.");
        if (string.IsNullOrWhiteSpace(settings.User))
            throw new ArgumentException("El usuario es obligatorio.");
        if (settings.Port is 0 or > 65535)
            throw new ArgumentException("El puerto debe estar entre 1 y 65535.");

        var builder = new MySqlConnectionStringBuilder
        {
            Server = settings.Server.Trim(),
            Port = settings.Port,
            Database = settings.Database.Trim(),
            UserID = settings.User.Trim(),
            Password = settings.Password,
            ConnectionTimeout = 5
        };

        return builder.ConnectionString;
    }

    public async Task<DatabaseConnectionResult> TestCurrentAsync()
    {
        return await TestAsync(CurrentConnectionString);
    }

    public static async Task<DatabaseConnectionResult> TestAsync(string connectionString)
    {
        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString)
            {
                ConnectionTimeout = 5
            };

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(7));
            await using var connection = new MySqlConnection(builder.ConnectionString);
            await connection.OpenAsync(timeout.Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(timeout.Token);

            return DatabaseConnectionResult.Ok();
        }
        catch (OperationCanceledException)
        {
            return DatabaseConnectionResult.Fail(
                "La conexión tardó demasiado. Verifica el servidor, el puerto y la red.");
        }
        catch (MySqlException ex)
        {
            return DatabaseConnectionResult.Fail(
                $"No fue posible conectar con MySQL. Verifica el servidor, la base de datos, el usuario y la contraseña.\n\nDetalle: {ex.Message}");
        }
        catch (Exception ex)
        {
            return DatabaseConnectionResult.Fail(
                $"La configuración de conexión no es válida.\n\nDetalle: {ex.Message}");
        }
    }

    public void Save(string connectionString)
    {
        _settings.Set("MySqlConnection", connectionString);
    }
}

public sealed record DatabaseConnectionSettings(
    string Server,
    uint Port,
    string Database,
    string User,
    string Password);

public sealed record DatabaseConnectionResult(bool Success, string Message)
{
    public static DatabaseConnectionResult Ok() =>
        new(true, "Conexión establecida correctamente.");

    public static DatabaseConnectionResult Fail(string message) =>
        new(false, message);
}
