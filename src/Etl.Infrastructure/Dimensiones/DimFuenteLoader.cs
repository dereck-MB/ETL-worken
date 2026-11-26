using System.Data;
using Etl.Application.Configuracion;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Dimensiones;

public sealed class DimFuenteLoader : DimensionLoaderBase
{
    private const string SqlOltp = "SELECT FuenteID, Nombre, Tipo FROM dbo.Fuentes";
    private const string SqlStaging = "SELECT DISTINCT Canal, Origen FROM stg.OpinionesExtraidas WHERE Canal IS NOT NULL";

    private sealed record OrigenFuente(int Id, string? Nombre, string? Tipo);
    private sealed record CanalStaging(string? Canal, string? Origen);
    private sealed record Fila(string Nk, int? IdOrigen, string Nombre, string Tipo, string Origen);

    public DimFuenteLoader(IOptions<DataWarehouseOptions> opciones) : base(opciones) { }

    public override string Dimension => "DimFuente";
    public override int Orden => 3;
    protected override string TablaPaso => "etl.Stg_DimFuente";
    protected override string Procedimiento => "etl.sp_Cargar_DimFuente";

    protected override async Task<DataTable> ExtraerYTransformarAsync(CancellationToken ct)
    {
        var filas = new Dictionary<string, Fila>();

        var oltp = await LectorSql.LeerAsync(Opciones.OrigenConnectionString, SqlOltp,
            rd => new OrigenFuente(rd.GetInt32(0), rd.TxtN(1), rd.TxtN(2)), Opciones.TimeoutSegundos, ct);

        foreach (var s in oltp)
        {
            var nombre = Transformar.Texto(s.Nombre);
            if (nombre is null) continue;
            var nk = Transformar.Clave(nombre);
            if (filas.ContainsKey(nk)) continue;

            filas[nk] = new Fila(nk, s.Id, nombre, Transformar.Texto(s.Tipo) ?? Transformar.SinDefinir, "OLTP");
        }

        if (Opciones.IncluirStaging)
        {
            var staging = await LectorSql.LeerAsync(Opciones.OrigenConnectionString, SqlStaging,
                rd => new CanalStaging(rd.TxtN(0), rd.TxtN(1)), Opciones.TimeoutSegundos, ct);

            foreach (var c in staging)
            {
                var nombre = Transformar.Texto(c.Canal);
                if (nombre is null) continue;
                var nk = Transformar.Clave(nombre);

                if (filas.TryGetValue(nk, out var existente))
                {
                    if (existente.Origen == "OLTP") filas[nk] = existente with { Origen = "AMBOS" };
                }
                else
                {
                    filas[nk] = new Fila(nk, null, nombre, Transformar.TipoCanal(nombre, c.Origen), "STAGING");
                }
            }
        }

        var t = new DataTable();
        t.Columns.Add("FuenteNK", typeof(string));
        t.Columns.Add("FuenteIDOrigen", typeof(int));
        t.Columns.Add("Nombre", typeof(string));
        t.Columns.Add("TipoCanal", typeof(string));
        t.Columns.Add("OrigenDato", typeof(string));

        foreach (var f in filas.Values)
            t.Rows.Add(f.Nk, Db(f.IdOrigen), f.Nombre, f.Tipo, f.Origen);

        return t;
    }
}
