using System.Threading.Tasks;

namespace ChecadorSSSD.Services;

/// <summary>
/// Servicio de autenticacion usando la BD de administradores con hash SHA256.
/// </summary>
public class AuthService
{
    private readonly AdministradorService _adminService;
    private bool _isAuthenticated = false;

    public bool IsAuthenticated => _isAuthenticated;

    public AuthService(AdministradorService adminService)
    {
        _adminService = adminService;
    }

    public async Task<bool> Login(string user, string pass)
    {
        var admin = await _adminService.ValidarCredencialesAsync(user, pass);
        _isAuthenticated = admin != null;
        return _isAuthenticated;
    }

    public void Logout()
    {
        _isAuthenticated = false;
    }
}
