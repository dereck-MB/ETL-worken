namespace Etl.Domain.Entidades;

public sealed record ResumenExtraccion(
    Guid LoteId,
    string Origen,
    int Leidos,
    int Validos,
    int Rechazados,
    long DuracionMs,
    string Estado,
    string? Mensaje);
