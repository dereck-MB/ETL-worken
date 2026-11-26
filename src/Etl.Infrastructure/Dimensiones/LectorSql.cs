using Microsoft.Data.SqlClient;

namespace Etl.Infrastructure.Dimensiones;

internal static class LectorSql
{
    public static async Task<List<T>> LeerAsync<T>(
        string cadena, string sql, Func<SqlDataReader, T> mapa, int timeoutSegundos, CancellationToken ct)
    {
        var lista = new List<T>();
        await using var cn = new SqlConnection(cadena);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn) { CommandTimeout = timeoutSegundos };
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
            lista.Add(mapa(rd));
        return lista;
    }

    public static string? TxtN(this SqlDataReader rd, int i) => rd.IsDBNull(i) ? null : rd.GetString(i);
    public static int? IntN(this SqlDataReader rd, int i) => rd.IsDBNull(i) ? null : rd.GetInt32(i);
    public static byte? ByteN(this SqlDataReader rd, int i) => rd.IsDBNull(i) ? null : rd.GetByte(i);
    public static decimal? DecN(this SqlDataReader rd, int i) => rd.IsDBNull(i) ? null : rd.GetDecimal(i);
    public static DateTime? FechaN(this SqlDataReader rd, int i) => rd.IsDBNull(i) ? null : rd.GetDateTime(i);
}
