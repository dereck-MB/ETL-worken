using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Etl.Application.Abstracciones;
using Etl.Application.Configuracion;
using Etl.Domain.Entidades;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Extractores;

public sealed class ComentarioApiDto
{
    public string? Id { get; set; }
    public string? Usuario { get; set; }
    public string? Correo { get; set; }
    public string? Producto { get; set; }
    public string? Plataforma { get; set; }
    public int? Calificacion { get; set; }
    public string? Texto { get; set; }
    public string? Fecha { get; set; }
}

public sealed class PaginaApiDto
{
    public int Pagina { get; set; }
    public int Total { get; set; }
    public bool HayMas { get; set; }
    public List<ComentarioApiDto> Datos { get; set; } = new();
}

public sealed class ApiExtractor : IExtractor
{
    public const string NombreCliente = "OpinionesApi";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _fabrica;
    private readonly ApiOptions _opciones;
    private readonly ILogger<ApiExtractor> _log;

    public ApiExtractor(IHttpClientFactory fabrica, IOptions<ApiOptions> opciones, ILogger<ApiExtractor> log)
    {
        _fabrica = fabrica;
        _opciones = opciones.Value;
        _log = log;
    }

    public string Origen => "API_REST";
    public bool Habilitado => _opciones.Habilitada;

    public async IAsyncEnumerable<RegistroCrudo> ExtraerAsync([EnumeratorCancellation] CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opciones.ApiKey))
            throw new InvalidOperationException(
                "Falta Fuentes:Api:ApiKey. Configurela con user-secrets o variable de entorno.");

        var http = _fabrica.CreateClient(NombreCliente);
        var pagina = 1;
        var hayMas = true;

        while (hayMas)
        {
            var resp = await ObtenerPaginaAsync(http, pagina, ct);

            foreach (var c in resp.Datos)
            {
                yield return new RegistroCrudo
                {
                    Origen = Origen,
                    IdExterno = c.Id,
                    Cliente = c.Usuario,
                    Correo = c.Correo,
                    Producto = c.Producto,
                    Canal = c.Plataforma,
                    Calificacion = c.Calificacion?.ToString(),
                    Comentario = c.Texto,
                    Fecha = c.Fecha
                };
            }

            hayMas = resp.HayMas;
            pagina++;
        }
    }

    private async Task<PaginaApiDto> ObtenerPaginaAsync(HttpClient http, int pagina, CancellationToken ct)
    {
        var url = $"api/comentarios?pagina={pagina}&tamano={_opciones.TamanoPagina}";

        for (var intento = 1; ; intento++)
        {
            try
            {
                var r = await http.GetFromJsonAsync<PaginaApiDto>(url, Json, ct);
                return r ?? throw new InvalidOperationException("El API devolvio una respuesta vacia");
            }
            catch (Exception ex) when (intento <= _opciones.Reintentos && EsTransitorio(ex, ct))
            {
                var espera = TimeSpan.FromSeconds(Math.Pow(2, intento));
                _log.LogWarning("[API_REST] Pagina {Pagina} fallo (intento {Intento}): {Error}. Reintento en {Seg}s",
                    pagina, intento, ex.Message, espera.TotalSeconds);
                await Task.Delay(espera, ct);
            }
        }
    }

    private static bool EsTransitorio(Exception ex, CancellationToken ct) => ex switch
    {
        HttpRequestException h => h.StatusCode is null || (int)h.StatusCode >= 500,
        TaskCanceledException => !ct.IsCancellationRequested,
        _ => false
    };
}
