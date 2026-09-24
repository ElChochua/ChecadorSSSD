using System;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChecadorSSSD.Services;

/// <summary>
/// Modelo para imagenes del fotorama.
/// </summary>
public class ImagenCarrusel
{
    public string Ruta { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    // Propiedad de conveniencia para binding de imagen preview en el carrusel
    public string? RutaPreview
    {
        get
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Ruta)) return null;
                if (!File.Exists(Ruta)) return null;
                return Ruta;
            }
            catch { return null; }
        }
    }
}

/// <summary>
/// Servicio que gestiona las imagenes configurables del programa.
/// </summary>
public class ImageService
{
    private readonly SettingsService _settings;
    private readonly string _baseDir;

    public string LogoChecadorPath => _settings.Get(nameof(LogoChecadorPath), string.Empty);
    public string ImagenLugarPath => _settings.Get(nameof(ImagenLugarPath), string.Empty);
    public string ImagenUniversidadPath => _settings.Get(nameof(ImagenUniversidadPath), string.Empty);
    public List<ImagenCarrusel> ImagenesCarrusel => ObtenerImagenesCarrusel();

    public ImageService()
    {
        _settings = new SettingsService();
        _baseDir = AppDomain.CurrentDomain.BaseDirectory;
    }

    public Bitmap? LoadImage(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(_baseDir, path);
            if (!File.Exists(fullPath)) return null;
            return new Bitmap(fullPath);
        }
        catch
        {
            return null;
        }
    }

    public void SetLogoChecador(string path) => _settings.Set(nameof(LogoChecadorPath), path);
    public void SetImagenLugar(string path) => _settings.Set(nameof(ImagenLugarPath), path);
    public void SetImagenUniversidad(string path) => _settings.Set(nameof(ImagenUniversidadPath), path);

    // Carrusel sidebar
    public List<ImagenCarrusel> ObtenerImagenesCarrusel()
    {
        var json = _settings.Get("CarruselImages", "[]");
        try
        {
            return JsonSerializer.Deserialize<List<ImagenCarrusel>>(json) ?? new List<ImagenCarrusel>();
        }
        catch
        {
            return new List<ImagenCarrusel>();
        }
    }

    public void GuardarImagenesCarrusel(List<ImagenCarrusel> imagenes)
    {
        var json = JsonSerializer.Serialize(imagenes);
        _settings.Set("CarruselImages", json);
    }

    public void AgregarImagenCarrusel(string ruta, string nombre)
    {
        var lista = ObtenerImagenesCarrusel();
        lista.Add(new ImagenCarrusel { Ruta = ruta, Nombre = nombre });
        GuardarImagenesCarrusel(lista);
    }

    public void EliminarImagenCarrusel(int indice)
    {
        var lista = ObtenerImagenesCarrusel();
        if (indice >= 0 && indice < lista.Count)
        {
            lista.RemoveAt(indice);
            GuardarImagenesCarrusel(lista);
        }
    }
}
