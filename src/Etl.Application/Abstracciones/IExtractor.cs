using Etl.Domain.Entidades;

namespace Etl.Application.Abstracciones;

public interface IExtractor
{
    string Origen { get; }
    bool Habilitado { get; }
    IAsyncEnumerable<RegistroCrudo> ExtraerAsync(CancellationToken ct);
}
