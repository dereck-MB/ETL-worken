using System.Data;
using Etl.Application.Configuracion;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Dimensiones;

public sealed class DimProductoLoader : DimensionLoaderBase
{
    private const string SqlOltp = "SELECT ProductoID, Nombre, Descripcion, Categoria, Precio FROM dbo.Productos";
    private const string SqlStaging = "SELECT DISTINCT Producto FROM stg.OpinionesExtraidas WHERE Producto IS NOT NULL";

    private sealed record OrigenProducto(int Id, string? Nombre, string? Descripcion, string? Categoria, decimal? Precio);
    private sealed record Fila(string Nk, int? IdOrigen, string Nombre, string? Descripcion, string Categoria, decimal? Precio, string Rango, string Origen);

    public DimProductoLoader(IOptions<DataWarehouseOptions> opciones) : base(opciones) { }

    public override string Dimension => "DimProducto";
    public override int Orden => 2;
    protected override string TablaPaso => "etl.Stg_DimProducto";
    protected override string Procedimiento => "etl.sp_Cargar_DimProducto";

    protected override async Task<DataTable> ExtraerYTransformarAsync(CancellationToken ct)
    {
        var filas = new Dictionary<string, Fila>();

        var oltp = await LectorSql.LeerAsync(Opciones.OrigenConnectionString, SqlOltp,
            rd => new OrigenProducto(rd.GetInt32(0), rd.TxtN(1), rd.TxtN(2), rd.TxtN(3), rd.DecN(4)),
            Opciones.TimeoutSegundos, ct);

        foreach (var p in oltp)
        {
            var nombre = Transformar.Texto(p.Nombre);
            if (nombre is null) continue;
            var nk = Transformar.Clave(nombre);
            if (filas.ContainsKey(nk)) continue;

            filas[nk] = new Fila(nk, p.Id, nombre, Transformar.Texto(p.Descripcion),
                Transformar.Texto(p.Categoria) ?? Transformar.SinDefinir, p.Precio,
                Transformar.RangoPrecio(p.Precio), "OLTP");
        }

        if (Opciones.IncluirStaging)
        {
            var staging = await LectorSql.LeerAsync(Opciones.OrigenConnectionString, SqlStaging,
                rd => rd.TxtN(0), Opciones.TimeoutSegundos, ct);

            foreach (var valor in staging)
            {
                var nombre = Transformar.Texto(valor);
                if (nombre is null) continue;
                var nk = Transformar.Clave(nombre);

                if (filas.TryGetValue(nk, out var existente))
                {
                    if (existente.Origen == "OLTP") filas[nk] = existente with { Origen = "AMBOS" };
                }
                else
                {
                    filas[nk] = new Fila(nk, null, nombre, null, Transformar.SinDefinir, null,
                        Transformar.RangoPrecio(null), "STAGING");
                }
            }
        }

        var t = new DataTable();
        t.Columns.Add("ProductoNK", typeof(string));
        t.Columns.Add("ProductoIDOrigen", typeof(int));
        t.Columns.Add("Nombre", typeof(string));
        t.Columns.Add("Descripcion", typeof(string));
        t.Columns.Add("Categoria", typeof(string));
        t.Columns.Add("Precio", typeof(decimal));
        t.Columns.Add("RangoPrecio", typeof(string));
        t.Columns.Add("OrigenDato", typeof(string));

        foreach (var f in filas.Values)
            t.Rows.Add(f.Nk, Db(f.IdOrigen), f.Nombre, Db(f.Descripcion), f.Categoria, Db(f.Precio), f.Rango, f.Origen);

        return t;
    }
}
