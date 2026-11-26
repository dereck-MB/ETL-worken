using System.Data;
using System.Globalization;
using Etl.Application.Configuracion;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Dimensiones;

public sealed class DimTiempoLoader : DimensionLoaderBase
{
    private static readonly string[] Meses =
        { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

    private static readonly string[] Dias =
        { "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado", "Domingo" };

    public DimTiempoLoader(IOptions<DataWarehouseOptions> opciones) : base(opciones) { }

    public override string Dimension => "DimTiempo";
    public override int Orden => 1;
    protected override string TablaPaso => "etl.Stg_DimTiempo";
    protected override string Procedimiento => "etl.sp_Cargar_DimTiempo";

    protected override Task<DataTable> ExtraerYTransformarAsync(CancellationToken ct)
    {
        var t = new DataTable();
        t.Columns.Add("TiempoKey", typeof(int));
        t.Columns.Add("Fecha", typeof(DateTime));
        t.Columns.Add("Anio", typeof(short));
        t.Columns.Add("Trimestre", typeof(byte));
        t.Columns.Add("Mes", typeof(byte));
        t.Columns.Add("NombreMes", typeof(string));
        t.Columns.Add("Dia", typeof(byte));
        t.Columns.Add("DiaSemana", typeof(byte));
        t.Columns.Add("NombreDia", typeof(string));
        t.Columns.Add("SemanaAnio", typeof(byte));
        t.Columns.Add("EsFinDeSemana", typeof(bool));

        var desde = new DateTime(Opciones.TiempoAnioDesde, 1, 1);
        var hasta = new DateTime(Opciones.TiempoAnioHasta, 12, 31);

        for (var f = desde; f <= hasta; f = f.AddDays(1))
        {
            var iso = ((int)f.DayOfWeek + 6) % 7;
            t.Rows.Add(
                f.Year * 10000 + f.Month * 100 + f.Day,
                f,
                (short)f.Year,
                (byte)((f.Month - 1) / 3 + 1),
                (byte)f.Month,
                Meses[f.Month - 1],
                (byte)f.Day,
                (byte)(iso + 1),
                Dias[iso],
                (byte)ISOWeek.GetWeekOfYear(f),
                iso >= 5);
        }

        return Task.FromResult(t);
    }
}
