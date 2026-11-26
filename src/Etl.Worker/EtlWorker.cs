using Etl.Application.Configuracion;
using Etl.Application.Servicios;
using Microsoft.Extensions.Options;

namespace Etl.Worker;

public sealed class EtlWorker : BackgroundService
{
    private readonly ExtraccionOrquestador _extraccion;
    private readonly CargaDimensionesOrquestador _dimensiones;
    private readonly EtlOptions _opciones;
    private readonly IHostApplicationLifetime _vida;
    private readonly ILogger<EtlWorker> _log;

    public EtlWorker(
        ExtraccionOrquestador extraccion,
        CargaDimensionesOrquestador dimensiones,
        IOptions<EtlOptions> opciones,
        IHostApplicationLifetime vida,
        ILogger<EtlWorker> log)
    {
        _extraccion = extraccion;
        _dimensiones = dimensiones;
        _opciones = opciones.Value;
        _vida = vida;
        _log = log;
    }

    private async Task EjecutarCicloAsync(CancellationToken ct)
    {
        var modo = _opciones.Modo;
        var extraccion = modo.Equals("Extraccion", StringComparison.OrdinalIgnoreCase);
        var dimensiones = modo.Equals("Dimensiones", StringComparison.OrdinalIgnoreCase);
        var todo = modo.Equals("Todo", StringComparison.OrdinalIgnoreCase);

        _log.LogInformation("Modo de ejecucion: {Modo}", modo);

        if (extraccion || todo)
            await _extraccion.EjecutarAsync(ct);

        if (dimensiones || todo)
            await _dimensiones.EjecutarAsync(ct);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await EjecutarCicloAsync(ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogError(ex, "La ejecucion del ETL fallo");
                }

                if (_opciones.EjecutarUnaVez) break;

                _log.LogInformation("Proxima ejecucion en {Min} minutos", _opciones.IntervaloMinutos);
                await Task.Delay(TimeSpan.FromMinutes(_opciones.IntervaloMinutos), ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_opciones.EjecutarUnaVez)
            {
                if (_opciones.PausarAlFinalizar && !Console.IsInputRedirected)
                {
                    Console.WriteLine();
                    Console.WriteLine("Presione una tecla para salir...");
                    Console.ReadKey(true);
                }
                _vida.StopApplication();
            }
        }
    }
}
