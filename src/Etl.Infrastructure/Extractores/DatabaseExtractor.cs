using System.Globalization;
using System.Runtime.CompilerServices;
using Etl.Application.Abstracciones;
using Etl.Application.Configuracion;
using Etl.Domain.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Extractores;

public sealed class DatabaseExtractor : IExtractor
{
    private const string Consulta = @"
SELECT ResenaID, Cliente, Correo, Producto, Plataforma, Calificacion, Texto, FechaResena
FROM dbo.Resenas
ORDER BY ResenaID";

    private readonly BaseDatosOptions _opciones;

    public DatabaseExtractor(IOptions<BaseDatosOptions> opciones) => _opciones = opciones.Value;

    public string Origen => "BASE_DATOS";
    public bool Habilitado => _opciones.Habilitada;

    public async IAsyncEnumerable<RegistroCrudo> ExtraerAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await using var cn = new SqlConnection(_opciones.ConnectionString);
        await cn.OpenAsync(ct);

        await using var cmd = new SqlCommand(Consulta, cn) { CommandTimeout = _opciones.TimeoutSegundos };
        await using var rd = await cmd.ExecuteReaderAsync(ct);

        while (await rd.ReadAsync(ct))
        {
            yield return new RegistroCrudo
            {
                Origen = Origen,
                IdExterno = "DB-" + rd.GetInt32(0).ToString(CultureInfo.InvariantCulture),
                Cliente = Texto(rd, 1),
                Correo = Texto(rd, 2),
                Producto = Texto(rd, 3),
                Canal = Texto(rd, 4),
                Calificacion = rd.GetInt32(5).ToString(CultureInfo.InvariantCulture),
                Comentario = Texto(rd, 6),
                Fecha = rd.GetDateTime(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
        }
    }

    private static string? Texto(SqlDataReader rd, int i) => rd.IsDBNull(i) ? null : rd.GetString(i);
}
