using System.Data;
using Etl.Application.Configuracion;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Dimensiones;

public sealed class DimSentimientoLoader : DimensionLoaderBase
{
    public DimSentimientoLoader(IOptions<DataWarehouseOptions> opciones) : base(opciones) { }

    public override string Dimension => "DimSentimiento";
    public override int Orden => 5;
    protected override string TablaPaso => "etl.Stg_DimSentimiento";
    protected override string Procedimiento => "etl.sp_Cargar_DimSentimiento";

    protected override Task<DataTable> ExtraerYTransformarAsync(CancellationToken ct)
    {
        var t = new DataTable();
        t.Columns.Add("SentimientoKey", typeof(byte));
        t.Columns.Add("Sentimiento", typeof(string));
        t.Columns.Add("RangoCalificacion", typeof(string));
        t.Columns.Add("Descripcion", typeof(string));

        t.Rows.Add((byte)0, "Sin clasificar", "N/A", "Opinion sin sentimiento asignado");
        t.Rows.Add((byte)1, "Positivo", "4-5", "Calificacion de 4 o 5");
        t.Rows.Add((byte)2, "Neutral", "3", "Calificacion de 3");
        t.Rows.Add((byte)3, "Negativo", "1-2", "Calificacion de 1 o 2");

        return Task.FromResult(t);
    }
}
