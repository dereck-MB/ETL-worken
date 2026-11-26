namespace Etl.Application.Configuracion;

public sealed class EtlOptions
{
    public const string Seccion = "Etl";
    public int TamanoLote { get; set; } = 500;
    public bool EjecutarUnaVez { get; set; } = true;
    public int IntervaloMinutos { get; set; } = 60;
    public bool LimpiarStagingAlIniciar { get; set; } = true;
    public bool PausarAlFinalizar { get; set; } = false;
    public string Modo { get; set; } = "Dimensiones";
}

public sealed class StagingOptions
{
    public const string Seccion = "Staging";
    public string ConnectionString { get; set; } = "";
}

public sealed class CsvOptions
{
    public const string Seccion = "Fuentes:Csv";
    public bool Habilitada { get; set; } = true;
    public string Ruta { get; set; } = "data/encuestas_internas.csv";
}

public sealed class BaseDatosOptions
{
    public const string Seccion = "Fuentes:BaseDatos";
    public bool Habilitada { get; set; } = true;
    public string ConnectionString { get; set; } = "";
    public int TimeoutSegundos { get; set; } = 30;
}

public sealed class ApiOptions
{
    public const string Seccion = "Fuentes:Api";
    public bool Habilitada { get; set; } = true;
    public string UrlBase { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int TamanoPagina { get; set; } = 50;
    public int Reintentos { get; set; } = 3;
    public int TimeoutSegundos { get; set; } = 15;
}

public sealed class DataWarehouseOptions
{
    public const string Seccion = "DataWarehouse";
    public string ConnectionString { get; set; } = "";
    public string OrigenConnectionString { get; set; } = "";
    public bool IncluirStaging { get; set; } = true;
    public int TiempoAnioDesde { get; set; } = 2025;
    public int TiempoAnioHasta { get; set; } = 2027;
    public int TimeoutSegundos { get; set; } = 60;
}
