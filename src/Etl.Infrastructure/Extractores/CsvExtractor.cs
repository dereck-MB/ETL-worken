using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Etl.Application.Abstracciones;
using Etl.Application.Configuracion;
using Etl.Domain.Entidades;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Extractores;

public sealed class FilaEncuestaCsv
{
    [Name("id_respuesta")] public string? IdRespuesta { get; set; }
    [Name("encuesta")] public string? Encuesta { get; set; }
    [Name("correo_cliente")] public string? CorreoCliente { get; set; }
    [Name("producto")] public string? Producto { get; set; }
    [Name("calificacion")] public string? Calificacion { get; set; }
    [Name("comentario")] public string? Comentario { get; set; }
    [Name("fecha")] public string? Fecha { get; set; }
}

public sealed class CsvExtractor : IExtractor
{
    private readonly CsvOptions _opciones;

    public CsvExtractor(IOptions<CsvOptions> opciones) => _opciones = opciones.Value;

    public string Origen => "CSV";
    public bool Habilitado => _opciones.Habilitada;

    public async IAsyncEnumerable<RegistroCrudo> ExtraerAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var ruta = Path.IsPathRooted(_opciones.Ruta)
            ? _opciones.Ruta
            : Path.Combine(AppContext.BaseDirectory, _opciones.Ruta);

        if (!File.Exists(ruta))
            throw new FileNotFoundException($"No se encontro el archivo CSV: {ruta}", ruta);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var lector = new StreamReader(ruta, Encoding.UTF8);
        using var csv = new CsvReader(lector, config);

        await foreach (var f in csv.GetRecordsAsync<FilaEncuestaCsv>(ct))
        {
            yield return new RegistroCrudo
            {
                Origen = Origen,
                IdExterno = f.IdRespuesta,
                Canal = f.Encuesta,
                Correo = f.CorreoCliente,
                Producto = f.Producto,
                Calificacion = f.Calificacion,
                Comentario = f.Comentario,
                Fecha = f.Fecha
            };
        }
    }
}
