using System.Globalization;
using System.Text.RegularExpressions;

namespace Etl.Infrastructure.Dimensiones;

internal static class Transformar
{
    public const string SinDefinir = "Sin definir";

    public static string? Texto(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var limpio = Regex.Replace(valor.Trim(), @"\s+", " ");
        return limpio.Length == 0 ? null : limpio;
    }

    public static string? Correo(string? valor)
    {
        var t = Texto(valor)?.ToLowerInvariant();
        return t is not null && t.Contains('@') && !t.Contains(' ') ? t : null;
    }

    public static string Clave(string valor) => valor.Trim().ToLowerInvariant();

    public static string TituloDesdeCorreo(string correo)
    {
        var local = correo.Split('@')[0].Replace('.', ' ').Replace('_', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(local);
    }

    public static string RangoEdad(byte? edad) => edad switch
    {
        null => SinDefinir,
        < 18 => "Menor de 18",
        <= 25 => "18-25",
        <= 35 => "26-35",
        <= 50 => "36-50",
        _ => "51 o mas"
    };

    public static string RangoPrecio(decimal? precio) => precio switch
    {
        null => SinDefinir,
        < 500m => "Economico",
        < 2000m => "Medio",
        _ => "Premium"
    };

    public static string TipoCanal(string nombre, string? origen)
    {
        if (string.Equals(origen, "CSV", StringComparison.OrdinalIgnoreCase)) return "Encuesta Interna";

        var n = nombre.ToLowerInvariant();
        if (n.Contains("instagram") || n.Contains("facebook") || n.Contains("twitter") ||
            n.Contains("tiktok") || n == "x") return "Red Social";
        if (n.Contains("google")) return "Resena Publica";
        if (n.Contains("whatsapp")) return "Mensajeria";
        if (n.Contains("web") || n.Contains("tienda")) return "Web";
        return SinDefinir;
    }
}
