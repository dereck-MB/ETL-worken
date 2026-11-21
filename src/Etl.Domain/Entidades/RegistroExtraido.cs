namespace Etl.Domain.Entidades;

/// Registro validado, listo para staging.
public sealed class RegistroExtraido
{
    public required string Origen { get; init; }
    public required string IdExterno { get; init; }
    public string? Cliente { get; init; }
    public string? Correo { get; init; }
    public string? Producto { get; init; }
    public string? Canal { get; init; }
    public required byte Calificacion { get; init; }
    public required string Comentario { get; init; }
    public DateOnly? FechaOrigen { get; init; }
}
