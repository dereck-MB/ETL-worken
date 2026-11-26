using System.Data;
using Etl.Application.Configuracion;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Dimensiones;

public sealed class DimClienteLoader : DimensionLoaderBase
{
    private const string SqlOltp =
        "SELECT ClienteID, Nombre, Correo, Pais, Edad, TipoCliente, FechaRegistro FROM dbo.Clientes";

    private const string SqlStaging =
        "SELECT Correo, MAX(Cliente) AS Cliente FROM stg.OpinionesExtraidas WHERE Correo IS NOT NULL GROUP BY Correo";

    private sealed record OrigenCliente(int Id, string? Nombre, string? Correo, string? Pais, byte? Edad, string? Tipo, DateTime? Fecha);
    private sealed record ClienteStaging(string? Correo, string? Nombre);
    private sealed record Fila(string Nk, int? IdOrigen, string Nombre, string? Correo, string Pais, byte? Edad,
        string RangoEdad, string Tipo, DateTime? FechaRegistro, string Origen);

    public DimClienteLoader(IOptions<DataWarehouseOptions> opciones) : base(opciones) { }

    public override string Dimension => "DimCliente";
    public override int Orden => 4;
    protected override string TablaPaso => "etl.Stg_DimCliente";
    protected override string Procedimiento => "etl.sp_Cargar_DimCliente";

    protected override async Task<DataTable> ExtraerYTransformarAsync(CancellationToken ct)
    {
        var filas = new Dictionary<string, Fila>();

        var oltp = await LectorSql.LeerAsync(Opciones.OrigenConnectionString, SqlOltp,
            rd => new OrigenCliente(rd.GetInt32(0), rd.TxtN(1), rd.TxtN(2), rd.TxtN(3), rd.ByteN(4), rd.TxtN(5), rd.FechaN(6)),
            Opciones.TimeoutSegundos, ct);

        foreach (var c in oltp)
        {
            var correo = Transformar.Correo(c.Correo);
            var nk = correo ?? $"SINCORREO-{c.Id}";
            if (filas.ContainsKey(nk)) continue;

            filas[nk] = new Fila(nk, c.Id, Transformar.Texto(c.Nombre) ?? "Sin nombre", correo,
                Transformar.Texto(c.Pais) ?? Transformar.SinDefinir, c.Edad, Transformar.RangoEdad(c.Edad),
                Transformar.Texto(c.Tipo) ?? Transformar.SinDefinir, c.Fecha, "OLTP");
        }

        if (Opciones.IncluirStaging)
        {
            var staging = await LectorSql.LeerAsync(Opciones.OrigenConnectionString, SqlStaging,
                rd => new ClienteStaging(rd.TxtN(0), rd.TxtN(1)), Opciones.TimeoutSegundos, ct);

            foreach (var s in staging)
            {
                var correo = Transformar.Correo(s.Correo);
                if (correo is null) continue;

                if (filas.TryGetValue(correo, out var existente))
                {
                    if (existente.Origen == "OLTP") filas[correo] = existente with { Origen = "AMBOS" };
                }
                else
                {
                    filas[correo] = new Fila(correo, null,
                        Transformar.Texto(s.Nombre) ?? Transformar.TituloDesdeCorreo(correo), correo,
                        Transformar.SinDefinir, null, Transformar.RangoEdad(null), Transformar.SinDefinir, null, "STAGING");
                }
            }
        }

        var t = new DataTable();
        t.Columns.Add("ClienteNK", typeof(string));
        t.Columns.Add("ClienteIDOrigen", typeof(int));
        t.Columns.Add("Nombre", typeof(string));
        t.Columns.Add("Correo", typeof(string));
        t.Columns.Add("Pais", typeof(string));
        t.Columns.Add("Edad", typeof(byte));
        t.Columns.Add("RangoEdad", typeof(string));
        t.Columns.Add("TipoCliente", typeof(string));
        t.Columns.Add("FechaRegistro", typeof(DateTime));
        t.Columns.Add("OrigenDato", typeof(string));

        foreach (var f in filas.Values)
            t.Rows.Add(f.Nk, Db(f.IdOrigen), f.Nombre, Db(f.Correo), f.Pais, Db(f.Edad), f.RangoEdad, f.Tipo, Db(f.FechaRegistro), f.Origen);

        return t;
    }
}
