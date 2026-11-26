namespace Etl.Domain.Entidades;

public sealed record ResultadoDimension(
    Guid LoteId,
    string Dimension,
    int Extraidos,
    int Insertados,
    int Actualizados,
    long DuracionMs,
    string Estado,
    string? Mensaje)
{
    public int SinCambio => Math.Max(Extraidos - Insertados - Actualizados, 0);
}
