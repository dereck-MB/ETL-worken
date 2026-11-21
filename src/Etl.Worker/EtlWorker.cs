using Etl.Application.Configuracion;
using Etl.Application.Servicios;
using Microsoft.Extensions.Options;

namespace Etl.Worker;

public sealed class EtlWorker : BackgroundService
{
    private readonly ExtraccionOrquestador _orquestador;
    private readonly EtlOptions _opciones;
    private readonly IHostApplicationLifetime _vida;
    private readonly ILogger<EtlWorker> _log;

    public EtlWorker(
        ExtraccionOrquestador orquestador,
        IOptions<EtlOptions> opciones,
        IHostApplicationLifetime vida,
        ILogger<EtlWorker> log)
    {
        _orquestador = orquestador;
        _opciones = opciones.Value;
        _vida = vida;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _orquestador.EjecutarAsync(ct);
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
