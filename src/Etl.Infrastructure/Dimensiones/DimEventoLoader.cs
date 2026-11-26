using System.Data;
using Etl.Application.Configuracion;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Dimensiones;

public sealed class DimEventoLoader : DimensionLoaderBase
{
    private const string SqlOltp = "SELECT EventoID, Nombre, Tipo, ProductoID, FechaInicio, FechaFin FROM dbo.Eventos";

    private sealed record OrigenEvento(int Id, string? Nombre, string? Tipo, int? ProductoId, DateTime? Inicio, DateTime? Fin);

    public DimEventoLoader(IOptions<DataWarehouseOptions> opciones) : base(opciones) { }

    public override string Dimension => "DimEvento";
    public override int Orden => 6;
    protected override string TablaPaso => "etl.Stg_DimEvento";
    protected override string Procedimiento => "etl.sp_Cargar_DimEvento";

    protected override async Task<DataTable> ExtraerYTransformarAsync(CancellationToken ct)
    {
        var eventos = await LectorSql.LeerAsync(Opciones.OrigenConnectionString, SqlOltp,
            rd => new OrigenEvento(rd.GetInt32(0), rd.TxtN(1), rd.TxtN(2), rd.IntN(3), rd.FechaN(4), rd.FechaN(5)),
            Opciones.TimeoutSegundos, ct);

        var t = new DataTable();
        t.Columns.Add("EventoIDOrigen", typeof(int));
        t.Columns.Add("Nombre", typeof(string));
        t.Columns.Add("Tipo", typeof(string));
        t.Columns.Add("ProductoIDOrigen", typeof(int));
        t.Columns.Add("FechaInicio", typeof(DateTime));
        t.Columns.Add("FechaFin", typeof(DateTime));
        t.Columns.Add("DuracionDias", typeof(int));

        foreach (var e in eventos)
        {
            int? duracion = e.Inicio.HasValue && e.Fin.HasValue ? (e.Fin.Value - e.Inicio.Value).Days + 1 : null;

            t.Rows.Add(e.Id,
                Transformar.Texto(e.Nombre) ?? "Sin nombre",
                Transformar.Texto(e.Tipo) ?? Transformar.SinDefinir,
                Db(e.ProductoId), Db(e.Inicio), Db(e.Fin), Db(duracion));
        }

        return t;
    }
}
