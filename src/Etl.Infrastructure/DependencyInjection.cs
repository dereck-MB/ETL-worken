using Etl.Application.Abstracciones;
using Etl.Application.Configuracion;
using Etl.Infrastructure.Dimensiones;
using Etl.Infrastructure.Extractores;
using Etl.Infrastructure.Persistencia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AgregarInfraestructura(this IServiceCollection servicios, IConfiguration config)
    {
        servicios.Configure<StagingOptions>(config.GetSection(StagingOptions.Seccion));
        servicios.Configure<CsvOptions>(config.GetSection(CsvOptions.Seccion));
        servicios.Configure<BaseDatosOptions>(config.GetSection(BaseDatosOptions.Seccion));
        servicios.Configure<ApiOptions>(config.GetSection(ApiOptions.Seccion));
        servicios.Configure<DataWarehouseOptions>(config.GetSection(DataWarehouseOptions.Seccion));

        servicios.AddHttpClient(ApiExtractor.NombreCliente, (sp, http) =>
        {
            var o = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
            http.BaseAddress = new Uri(o.UrlBase.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(o.TimeoutSegundos);
            http.DefaultRequestHeaders.Add("X-Api-Key", o.ApiKey);
        });

        servicios.AddSingleton<IStagingRepository, StagingRepository>();

        // Para agregar una fuente nueva: crear su IExtractor y registrarlo aqui.
        servicios.AddSingleton<IExtractor, CsvExtractor>();
        servicios.AddSingleton<IExtractor, DatabaseExtractor>();
        servicios.AddSingleton<IExtractor, ApiExtractor>();

        servicios.AddSingleton<IBitacoraDw, BitacoraDwRepository>();

        // Para agregar una dimension nueva: crear su IDimensionLoader y registrarlo aqui.
        servicios.AddSingleton<IDimensionLoader, DimTiempoLoader>();
        servicios.AddSingleton<IDimensionLoader, DimProductoLoader>();
        servicios.AddSingleton<IDimensionLoader, DimFuenteLoader>();
        servicios.AddSingleton<IDimensionLoader, DimClienteLoader>();
        servicios.AddSingleton<IDimensionLoader, DimSentimientoLoader>();
        servicios.AddSingleton<IDimensionLoader, DimEventoLoader>();

        return servicios;
    }
}
