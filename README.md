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

## Actividad 2: carga de dimensiones al DataWarehouse

El Worker ahora tambien carga las dimensiones de la base `DataWarehouse` (DimTiempo, DimCliente, DimProducto, DimFuente, DimSentimiento, DimEvento) desde la BD relacional y desde el staging. Usa tablas de paso `etl.Stg_Dim*` y procedimientos `MERGE`, asi que se puede ejecutar varias veces sin duplicar.

### Ejecucion

1. En SSMS ejecutar, en orden (cada script se abre y se ejecuta completo con F5):
   - `database/00_Origen_SistemaAnalisisOpiniones.sql` (base relacional con datos de ejemplo)
   - `database/05_Staging.sql`
   - `database/06_BD_Fuente_OpinionesExterno.sql`
   - `database/08_DataWarehouse.sql` (crea la BD `DataWarehouse`)
2. Si el servidor no es `localhost`, cambiar `Server=localhost;` en las 4 cadenas de `src/Etl.Worker/appsettings.json`. En JSON la barra se escribe doble, por ejemplo `Server=(localdb)\\MSSQLLocalDB;` o `Server=localhost\\SQLEXPRESS;`.
3. Llenar el staging: dejar corriendo el API y ejecutar el Worker en modo `Extraccion` (ver README de la actividad 1):
   ```
   dotnet run --project src/Etl.MockApi
   dotnet run --project src/Etl.Worker -- --Etl:Modo=Extraccion
   ```
   `SELECT COUNT(*) FROM stg.OpinionesExtraidas` debe dar 464.
4. Cargar las dimensiones (modo por defecto):
   ```
   dotnet run --project src/Etl.Worker
   ```
5. Verificar con `database/10_Consultas_Evidencia_DW.sql`.

Modos disponibles, por configuracion `Etl:Modo` o por linea de comandos:

| Modo | Que hace |
|---|---|
| `Dimensiones` | Carga las dimensiones (por defecto) |
| `Extraccion` | Extrae CSV, BD y API hacia staging |
| `Todo` | Extraccion y luego dimensiones |

Ejemplo: `dotnet run --project src/Etl.Worker -- --Etl:Modo=Todo`

### Nuevos componentes

- `Etl.Application`: `IDimensionLoader`, `IBitacoraDw`, `CargaDimensionesOrquestador`
- `Etl.Infrastructure/Dimensiones`: un cargador por dimension y `DimensionLoaderBase`
- `database/`: scripts 00 (origen con datos), 08 (DataWarehouse) y 10 (consultas de evidencia)
- `docs/`: diagramas `dw_*` y `Documento_Carga_Dimensiones.pdf`
