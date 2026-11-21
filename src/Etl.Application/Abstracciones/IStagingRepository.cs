using Etl.Domain.Entidades;

namespace Etl.Application.Abstracciones;

public interface IStagingRepository
{
    Task LimpiarAsync(CancellationToken ct);
    Task GuardarLoteAsync(Guid loteId, IReadOnlyList<RegistroExtraido> registros, CancellationToken ct);
    Task RegistrarResumenAsync(ResumenExtraccion resumen, CancellationToken ct);
}
