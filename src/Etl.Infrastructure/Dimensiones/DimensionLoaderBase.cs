using System.Data;
using System.Diagnostics;
using Etl.Application.Abstracciones;
using Etl.Application.Configuracion;
using Etl.Domain.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Dimensiones;

public abstract class DimensionLoaderBase : IDimensionLoader
{
    protected DimensionLoaderBase(IOptions<DataWarehouseOptions> opciones) => Opciones = opciones.Value;

    protected DataWarehouseOptions Opciones { get; }

    public abstract string Dimension { get; }
    public abstract int Orden { get; }
    protected abstract string TablaPaso { get; }
    protected abstract string Procedimiento { get; }

    protected abstract Task<DataTable> ExtraerYTransformarAsync(CancellationToken ct);

    public async Task<ResultadoDimension> CargarAsync(Guid loteId, CancellationToken ct)
    {
        var reloj = Stopwatch.StartNew();
        var tabla = await ExtraerYTransformarAsync(ct);

        await using var cn = new SqlConnection(Opciones.ConnectionString);
        await cn.OpenAsync(ct);
        await using var tx = (SqlTransaction)await cn.BeginTransactionAsync(ct);

        try
        {
            await using (var limpiar = new SqlCommand($"TRUNCATE TABLE {TablaPaso}", cn, tx))
                await limpiar.ExecuteNonQueryAsync(ct);

            using (var bulk = new SqlBulkCopy(cn, SqlBulkCopyOptions.Default, tx))
            {
                bulk.DestinationTableName = TablaPaso;
                bulk.BulkCopyTimeout = Opciones.TimeoutSegundos;
                foreach (DataColumn c in tabla.Columns)
                    bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);
                await bulk.WriteToServerAsync(tabla, ct);
            }

            var pIns = new SqlParameter("@Insertados", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var pAct = new SqlParameter("@Actualizados", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await using var cmd = new SqlCommand(Procedimiento, cn, tx)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = Opciones.TimeoutSegundos
            };
            cmd.Parameters.Add(pIns);
            cmd.Parameters.Add(pAct);
            await cmd.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);

            reloj.Stop();
            return new ResultadoDimension(loteId, Dimension, tabla.Rows.Count,
                (int)pIns.Value, (int)pAct.Value, reloj.ElapsedMilliseconds, "OK", null);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    protected static object Db(object? valor) => valor ?? DBNull.Value;
}
