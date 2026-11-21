using System.Globalization;
using Etl.Domain.Entidades;

namespace Etl.Application.Servicios;

public static class ValidadorRegistros
{
    private static readonly string[] Formatos = { "yyyy-MM-dd", "dd/MM/yyyy" };
    private const int MaxComentario = 1000;

    public static bool TryValidar(RegistroCrudo r, out RegistroExtraido? registro, out string motivo)
    {
        registro = null;
        motivo = "";

        var comentario = r.Comentario?.Trim();
        if (string.IsNullOrEmpty(comentario)) { motivo = "Comentario vacio"; return false; }

        if (!int.TryParse(r.Calificacion, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cal))
        { motivo = $"Calificacion invalida '{r.Calificacion}'"; return false; }
        if (cal < 1 || cal > 5) { motivo = $"Calificacion fuera de rango ({cal})"; return false; }

        var correo = r.Correo?.Trim();
        if (!string.IsNullOrEmpty(correo) && !CorreoValido(correo))
        { motivo = $"Correo invalido '{correo}'"; return false; }

        DateOnly? fecha = null;
        if (!string.IsNullOrWhiteSpace(r.Fecha))
        {
            if (!DateOnly.TryParseExact(r.Fecha.Trim(), Formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var f))
            { motivo = $"Fecha invalida '{r.Fecha}'"; return false; }
            fecha = f;
        }

        registro = new RegistroExtraido
        {
            Origen = r.Origen,
            IdExterno = string.IsNullOrWhiteSpace(r.IdExterno) ? "SIN-ID" : r.IdExterno.Trim(),
            Cliente = r.Cliente?.Trim(),
            Correo = string.IsNullOrEmpty(correo) ? null : correo.ToLowerInvariant(),
            Producto = r.Producto?.Trim(),
            Canal = r.Canal?.Trim(),
            Calificacion = (byte)cal,
            Comentario = comentario.Length > MaxComentario ? comentario[..MaxComentario] : comentario,
            FechaOrigen = fecha
        };
        return true;
    }

    private static bool CorreoValido(string c)
    {
        var i = c.IndexOf('@');
        return i > 0 && c.IndexOf('.', i) > i + 1 && !c.Contains(' ');
    }
}
