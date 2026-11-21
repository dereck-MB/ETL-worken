using System.Data;
using Etl.Application.Abstracciones;
using Etl.Application.Configuracion;
using Etl.Domain.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Persistencia;

public sealed class StagingRepository : IStagingRepository
{
    private const string Destino = "stg.OpinionesExtraidas";
    private readonly string _cadena;

    public StagingRepository(IOptions<StagingOptions> opciones) => _cadena = opciones.Value.ConnectionString;

    public async Task LimpiarAsync(CancellationToken ct)
    {
        await using var cn = new SqlConnection(_cadena);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand("stg.sp_LimpiarStaging", cn) { CommandType = CommandType.StoredProcedure };
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task GuardarLoteAsync(Guid loteId, IReadOnlyList<RegistroExtraido> registros, CancellationToken ct)
    {
        var tabla = new DataTable();
        tabla.Columns.Add("LoteID", typeof(Guid));
        tabla.Columns.Add("Origen", typeof(string));
        tabla.Columns.Add("IdExterno", typeof(string));
        tabla.Columns.Add("Cliente", typeof(string));
        tabla.Columns.Add("Correo", typeof(string));
        tabla.Columns.Add("Producto", typeof(string));
        tabla.Columns.Add("Canal", typeof(string));
        tabla.Columns.Add("Calificacion", typeof(byte));
        tabla.Columns.Add("Comentario", typeof(string));
        tabla.Columns.Add("FechaOrigen", typeof(DateTime));

        foreach (var r in registros)
        {
            tabla.Rows.Add(
                loteId,
                r.Origen,
                r.IdExterno,
                (object?)r.Cliente ?? DBNull.Value,
                (object?)r.Correo ?? DBNull.Value,
                (object?)r.Producto ?? DBNull.Value,
                (object?)r.Canal ?? DBNull.Value,
                r.Calificacion,
                r.Comentario,
                r.FechaOrigen.HasValue ? (object)r.FechaOrigen.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        }

        await using var cn = new SqlConnection(_cadena);
        await cn.OpenAsync(ct);

        using var bulk = new SqlBulkCopy(cn)
        {
            DestinationTableName = Destino,
            BulkCopyTimeout = 60
        };
        foreach (DataColumn c in tabla.Columns)
            bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);

        await bulk.WriteToServerAsync(tabla, ct);
    }

    public async Task RegistrarResumenAsync(ResumenExtraccion r, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO stg.LogExtraccion (LoteID, Origen, Leidos, Validos, Rechazados, DuracionMs, Estado, Mensaje)
VALUES (@lote, @origen, @leidos, @validos, @rechazados, @ms, @estado, @mensaje)";

        await using var cn = new SqlConnection(_cadena);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@lote", r.LoteId);
        cmd.Parameters.AddWithValue("@origen", r.Origen);
        cmd.Parameters.AddWithValue("@leidos", r.Leidos);
        cmd.Parameters.AddWithValue("@validos", r.Validos);
        cmd.Parameters.AddWithValue("@rechazados", r.Rechazados);
        cmd.Parameters.AddWithValue("@ms", r.DuracionMs);
        cmd.Parameters.AddWithValue("@estado", r.Estado);
        cmd.Parameters.AddWithValue("@mensaje", (object?)r.Mensaje ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
