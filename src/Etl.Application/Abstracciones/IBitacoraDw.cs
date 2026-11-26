using Etl.Domain.Entidades;

namespace Etl.Application.Abstracciones;

public interface IBitacoraDw
{
    Task RegistrarAsync(ResultadoDimension resultado, CancellationToken ct);
}
