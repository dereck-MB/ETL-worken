# EtlOpinionesWorker

Worker Service en .NET 8 que extrae opiniones de clientes desde CSV, base de datos relacional y API REST hacia tablas staging de `SistemaAnalisisOpiniones`.

## Estructura

```
src/
  Etl.Domain          Entidades (sin dependencias)
  Etl.Application     Interfaces, orquestador, validador, opciones
  Etl.Infrastructure  CsvExtractor, DatabaseExtractor, ApiExtractor, StagingRepository
  Etl.Worker          Punto de entrada (BackgroundService) y configuracion
  Etl.MockApi         API REST local de prueba
database/             Scripts SQL
data/                 CSV de encuestas internas
docs/                 Documento tecnico (PDF) y diagramas
```

## Requisitos

- .NET 8 SDK (o Visual Studio con .NET 8)
- SQL Server local con la base `SistemaAnalisisOpiniones` (script `01_Script_BaseDeDatos.sql` de la actividad anterior)

## Ejecucion

1. En SSMS ejecutar, en orden:
   - `database/05_Staging.sql`
   - `database/06_BD_Fuente_OpinionesExterno.sql`
2. Guardar la clave del API como secreto (no va en el repositorio):
   ```
   dotnet user-secrets set "Fuentes:Api:ApiKey" "dev-opiniones-key-2026" --project src/Etl.Worker
   ```
3. Levantar el API de prueba (dejar la ventana abierta):
   ```
   dotnet run --project src/Etl.MockApi
   ```
4. En otra terminal, correr el Worker:
   ```
   dotnet run --project src/Etl.Worker
   ```
5. Verificar en SSMS con `database/07_Consultas_Evidencia.sql`.

Si SQL Server no es `localhost` o usa usuario y clave, cambiar las cadenas en `src/Etl.Worker/appsettings.json` o crear `appsettings.Local.json` (ignorado por git).

## Resultado esperado

| Fuente | Leidos | Validos | Rechazados |
|---|---|---|---|
| CSV | 90 | 84 | 6 |
| BASE_DATOS | 240 | 235 | 5 |
| API_REST | 150 | 145 | 5 |
| Total | 480 | 464 | 16 |

## Configuracion

Todo se controla en `appsettings.json`: tamano de lote, ejecucion unica o periodica, y cada fuente (`Habilitada`, rutas, cadenas de conexion). Cada valor se puede sobrescribir con variables de entorno, por ejemplo `Fuentes__Api__ApiKey`.

## Agregar una fuente nueva

1. Crear una clase que implemente `IExtractor` en `Etl.Infrastructure/Extractores`.
2. Registrarla en `DependencyInjection.cs`.
3. Agregar su seccion de opciones y su bloque en `appsettings.json`.
