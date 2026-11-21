using Etl.Application.Configuracion;
using Etl.Application.Servicios;
using Etl.Infrastructure;
using Etl.Worker;
using Serilog;
using Serilog.Events;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "etl-.log"),
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

builder.Services.Configure<EtlOptions>(builder.Configuration.GetSection(EtlOptions.Seccion));
builder.Services.AgregarInfraestructura(builder.Configuration);
builder.Services.AddSingleton<ExtraccionOrquestador>();
builder.Services.AddHostedService<EtlWorker>();

await builder.Build().RunAsync();
