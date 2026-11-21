namespace Etl.Domain.Entidades;

/// Registro tal como llega de la fuente, sin validar.
public sealed class RegistroCrudo
{
    public required string Origen { get; init; }
    public string? IdExterno { get; init; }
    public string? Cliente { get; init; }
    public string? Correo { get; init; }
    public string? Producto { get; init; }
    public string? Canal { get; init; }
    public string? Calificacion { get; init; }
    public string? Comentario { get; init; }
    public string? Fecha { get; init; }
}
