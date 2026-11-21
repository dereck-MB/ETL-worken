using System.Diagnostics;
using Etl.Application.Abstracciones;
using Etl.Application.Configuracion;
using Etl.Domain.Entidades;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Etl.Application.Servicios;

public sealed class ExtraccionOrquestador
{
    private readonly IEnumerable<IExtractor> _extractores;
    private readonly IStagingRepository _staging;
    private readonly EtlOptions _opciones;
    private readonly ILogger<ExtraccionOrquestador> _log;

    public ExtraccionOrquestador(
        IEnumerable<IExtractor> extractores,
        IStagingRepository staging,
        IOptions<EtlOptions> opciones,
        ILogger<ExtraccionOrquestador> log)
    {
        _extractores = extractores;
        _staging = staging;
        _opciones = opciones.Value;
        _log = log;
    }

    public async Task<IReadOnlyList<ResumenExtraccion>> EjecutarAsync(CancellationToken ct)
    {
        var lote = Guid.NewGuid();
        var reloj = Stopwatch.StartNew();
        var activos = _extractores.Where(e => e.Habilitado).ToList();

        _log.LogInformation("Inicio de extraccion. Lote {Lote}. Fuentes activas: {Fuentes}",
            lote, string.Join(", ", activos.Select(e => e.Origen)));

        if (_opciones.LimpiarStagingAlIniciar)
        {
            await _staging.LimpiarAsync(ct);
            _log.LogInformation("Staging limpiado");
        }

        var resumenes = await Task.WhenAll(activos.Select(e => ProcesarOrigenAsync(e, lote, ct)));

        reloj.Stop();
        _log.LogInformation(
            "Extraccion finalizada en {Ms} ms. Leidos: {Leidos}, validos: {Validos}, rechazados: {Rechazados}",
            reloj.ElapsedMilliseconds, resumenes.Sum(r => r.Leidos), resumenes.Sum(r => r.Validos), resumenes.Sum(r => r.Rechazados));

        return resumenes;
    }

    private async Task<ResumenExtraccion> ProcesarOrigenAsync(IExtractor extractor, Guid lote, CancellationToken ct)
    {
        var reloj = Stopwatch.StartNew();
        int leidos = 0, validos = 0, rechazados = 0;
        var estado = "OK";
        string? mensaje = null;
        var pendientes = new List<RegistroExtraido>(_opciones.TamanoLote);

        _log.LogInformation("[{Origen}] Extrayendo...", extractor.Origen);

        try
        {
            await foreach (var crudo in extractor.ExtraerAsync(ct))
            {
                leidos++;
                if (ValidadorRegistros.TryValidar(crudo, out var registro, out var motivo))
                {
                    validos++;
                    pendientes.Add(registro!);
                    if (pendientes.Count >= _opciones.TamanoLote)
                    {
                        await _staging.GuardarLoteAsync(lote, pendientes, ct);
                        pendientes.Clear();
                    }
                }
                else
                {
                    rechazados++;
                    _log.LogWarning("[{Origen}] Registro {Id} rechazado: {Motivo}", extractor.Origen, crudo.IdExterno, motivo);
                }
            }

            if (pendientes.Count > 0)
                await _staging.GuardarLoteAsync(lote, pendientes, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            estado = "ERROR";
            mensaje = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            _log.LogError(ex, "[{Origen}] Fallo la extraccion", extractor.Origen);
        }

        reloj.Stop();
        var resumen = new ResumenExtraccion(lote, extractor.Origen, leidos, validos, rechazados, reloj.ElapsedMilliseconds, estado, mensaje);

        try
        {
            await _staging.RegistrarResumenAsync(resumen, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "[{Origen}] No se pudo registrar el resumen", extractor.Origen);
        }

        _log.LogInformation("[{Origen}] {Estado}: leidos {Leidos}, validos {Validos}, rechazados {Rechazados} ({Ms} ms)",
            extractor.Origen, estado, leidos, validos, rechazados, resumen.DuracionMs);

        return resumen;
    }
}
