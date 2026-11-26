using Etl.Domain.Entidades;

namespace Etl.Application.Abstracciones;

public interface IDimensionLoader
{
    string Dimension { get; }
    int Orden { get; }
    Task<ResultadoDimension> CargarAsync(Guid loteId, CancellationToken ct);
}
