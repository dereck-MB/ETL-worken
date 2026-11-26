using System.Diagnostics;
using Etl.Application.Abstracciones;
using Etl.Domain.Entidades;
using Microsoft.Extensions.Logging;

namespace Etl.Application.Servicios;

public sealed class CargaDimensionesOrquestador
{
    private readonly IEnumerable<IDimensionLoader> _cargadores;
    private readonly IBitacoraDw _bitacora;
    private readonly ILogger<CargaDimensionesOrquestador> _log;

    public CargaDimensionesOrquestador(
        IEnumerable<IDimensionLoader> cargadores,
        IBitacoraDw bitacora,
        ILogger<CargaDimensionesOrquestador> log)
    {
        _cargadores = cargadores;
        _bitacora = bitacora;
        _log = log;
    }

    public async Task<IReadOnlyList<ResultadoDimension>> EjecutarAsync(CancellationToken ct)
    {
        var lote = Guid.NewGuid();
        var total = Stopwatch.StartNew();
        var ordenados = _cargadores.OrderBy(c => c.Orden).ToList();
        var resultados = new List<ResultadoDimension>();

        _log.LogInformation("Inicio de carga de dimensiones. Lote {Lote}. Dimensiones: {Dims}",
            lote, string.Join(", ", ordenados.Select(c => c.Dimension)));

        foreach (var cargador in ordenados)
        {
            ct.ThrowIfCancellationRequested();
            var reloj = Stopwatch.StartNew();
            ResultadoDimension resultado;

            try
            {
                resultado = await cargador.CargarAsync(lote, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[{Dim}] Fallo la carga", cargador.Dimension);
                var mensaje = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                resultado = new ResultadoDimension(lote, cargador.Dimension, 0, 0, 0, reloj.ElapsedMilliseconds, "ERROR", mensaje);
            }

            try
            {
                await _bitacora.RegistrarAsync(resultado, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "[{Dim}] No se pudo registrar en la bitacora", cargador.Dimension);
            }

            _log.LogInformation(
                "[{Dim}] {Estado}: extraidos {Ext}, insertados {Ins}, actualizados {Act}, sin cambio {Sin} ({Ms} ms)",
                resultado.Dimension, resultado.Estado, resultado.Extraidos, resultado.Insertados,
                resultado.Actualizados, resultado.SinCambio, resultado.DuracionMs);

            resultados.Add(resultado);
        }

        total.Stop();
        _log.LogInformation("Carga de dimensiones finalizada en {Ms} ms. Insertados: {Ins}, actualizados: {Act}",
            total.ElapsedMilliseconds, resultados.Sum(r => r.Insertados), resultados.Sum(r => r.Actualizados));

        return resultados;
    }
}
