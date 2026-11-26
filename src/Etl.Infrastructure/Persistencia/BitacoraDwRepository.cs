using Etl.Application.Abstracciones;
using Etl.Application.Configuracion;
using Etl.Domain.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Persistencia;

public sealed class BitacoraDwRepository : IBitacoraDw
{
    private readonly string _cadena;

    public BitacoraDwRepository(IOptions<DataWarehouseOptions> opciones) => _cadena = opciones.Value.ConnectionString;

    public async Task RegistrarAsync(ResultadoDimension r, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO etl.LogCarga (LoteID, Dimension, Extraidos, Insertados, Actualizados, SinCambio, DuracionMs, Estado, Mensaje)
VALUES (@lote, @dim, @ext, @ins, @act, @sin, @ms, @estado, @mensaje)";

        await using var cn = new SqlConnection(_cadena);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@lote", r.LoteId);
        cmd.Parameters.AddWithValue("@dim", r.Dimension);
        cmd.Parameters.AddWithValue("@ext", r.Extraidos);
        cmd.Parameters.AddWithValue("@ins", r.Insertados);
        cmd.Parameters.AddWithValue("@act", r.Actualizados);
        cmd.Parameters.AddWithValue("@sin", r.SinCambio);
        cmd.Parameters.AddWithValue("@ms", r.DuracionMs);
        cmd.Parameters.AddWithValue("@estado", r.Estado);
        cmd.Parameters.AddWithValue("@mensaje", (object?)r.Mensaje ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
