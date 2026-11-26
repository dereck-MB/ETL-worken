import base64, html, os, re, sys
from pathlib import Path
from playwright.sync_api import sync_playwright

RAIZ = Path(__file__).resolve().parent.parent
DOCS = RAIZ / "docs"

def img(p):
    return "data:image/png;base64," + base64.b64encode(Path(p).read_bytes()).decode()

def codigo(ruta, ini=None, fin=None):
    lineas = (RAIZ / ruta).read_text(encoding="utf-8").splitlines()
    if ini: lineas = lineas[ini - 1: fin]
    return f'<pre>{html.escape(chr(10).join(lineas))}</pre>'

def captura(n, titulo, pie, opcional=False):
    f = DOCS / "capturas" / f"captura_{n}.png"
    if opcional and not f.exists():
        return ""
    if f.exists():
        cuerpo = f'<img class="cap" src="{img(f)}">'
    else:
        cuerpo = f'<div class="pend">Captura pendiente: {html.escape(titulo)}<br><small>Guardar como docs/capturas/captura_{n}.png</small></div>'
    return f'<figure>{cuerpo}<figcaption>Figura {n}. {html.escape(pie)}</figcaption></figure>'

CSS = """
@page { size: Letter; margin: 22mm 18mm; }
body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 10.5pt; color: #1b2430; line-height: 1.45; }
h1 { font-size: 20pt; margin: 0 0 4px; color: #111; }
h2 { font-size: 15pt; color: #111; border-bottom: 1px solid #111; padding-bottom: 3px; margin-top: 26px; page-break-after: avoid; }
h3 { font-size: 11.5pt; color: #333; margin: 14px 0 4px; page-break-after: avoid; }
.sub { color: #555; margin-bottom: 18px; }
.meta td { padding: 2px 14px 2px 0; }
table.t { border-collapse: collapse; width: 100%; margin: 8px 0; font-size: 9.5pt; }
table.t th { background: #e6e6e6; color: #111; text-align: left; padding: 5px 8px; }
table.t td { border: 1px solid #bbb; padding: 5px 8px; vertical-align: top; }
table.t tr:nth-child(even) td { background: #f7f7f7; }
pre { background: #f7f7f7; border: 1px solid #ccc; padding: 8px 10px; font-size: 7.8pt; line-height: 1.35; white-space: pre-wrap; margin: 0 0 10px; }
.arch { font-family: Consolas, monospace; font-size: 8.5pt; color: #fff; background: #333; display: inline-block; padding: 2px 8px; border-radius: 4px 4px 0 0; margin-top: 8px; }
figure { margin: 12px 0; text-align: center; page-break-inside: avoid; }
figure img { max-width: 100%; }
figure img.cap { border: 1px solid #bbb; }
figcaption { display: none; }
.pend { border: 2px dashed #b0bccd; color: #7a8899; padding: 60px 20px; border-radius: 6px; background: #fafbfd; }
.nota { background: #f2f2f2; border-left: 4px solid #333; padding: 6px 10px; margin: 8px 0; }
.salto { page-break-before: always; }
ul { margin: 4px 0 6px 18px; padding: 0; }
"""

def tabla(cab, filas):
    h = "".join(f"<th>{c}</th>" for c in cab)
    b = "".join("<tr>" + "".join(f"<td>{c}</td>" for c in f) + "</tr>" for f in filas)
    return f'<table class="t"><tr>{h}</tr>{b}</table>'

D = DOCS / "diagramas"
partes = []
partes.append(f"""
<h1>Proceso de Extracción (E de ETL) en .NET 8</h1>
<div class="sub">Sistema de Análisis de Opiniones de Clientes — Arquitectura y Worker Service</div>

<h2>1. Resumen</h2>
<p>Se construyó un <b>Worker Service en .NET 8</b> que extrae opiniones de clientes desde tres tipos de fuente (archivo CSV, base de datos relacional y API REST), las valida y las deja en tablas <i>staging</i> de la base analítica <b>SistemaAnalisisOpiniones</b>, modelada en la actividad anterior. El diseño sigue Clean Architecture y cada fuente es un componente intercambiable detrás de la interfaz <code>IExtractor</code>.</p>
{tabla(["Fuente","Tecnología","Clase","Contenido"],[
 ["CSV","CsvHelper","CsvExtractor","Encuestas internas (90 filas)"],
 ["Base de datos relacional","ADO.NET (SqlDataReader)","DatabaseExtractor","Reseñas de la BD OpinionesExterno (240 filas)"],
 ["API REST","IHttpClientFactory + System.Text.Json","ApiExtractor","Comentarios de redes sociales, paginados (150 filas)"]])}

<h2>2. Diseño de la arquitectura</h2>
<figure><img src="{img(D/"arquitectura.png")}" style="max-height:150mm"><figcaption>Figura A. Diagrama de arquitectura del proceso ETL.</figcaption></figure>
<h3>Componentes</h3>
{tabla(["Componente","Responsabilidad"],[
 ["Etl.Worker (BackgroundService)","Punto de entrada. Arranca la extracción una vez o de forma periódica según la configuración."],
 ["ExtraccionOrquestador","Ejecuta todas las fuentes habilitadas en paralelo, valida los registros, arma lotes y registra métricas."],
 ["IExtractor / Extractores","Un extractor por tipo de fuente; devuelve los registros en streaming (IAsyncEnumerable)."],
 ["ValidadorRegistros","Reglas de calidad: comentario obligatorio, calificación 1 a 5, correo y fecha válidos."],
 ["StagingRepository","Carga masiva con SqlBulkCopy a stg.OpinionesExtraidas y bitácora en stg.LogExtraccion."],
 ["BD analítica","SistemaAnalisisOpiniones, esquema <code>stg</code> con tabla de staging, bitácora y vista de resumen."],
 ["Dashboard","Consume la vista stg.vw_ResumenPorOrigen y las tablas del modelo (Power BI o consultas SQL)."]])}

<h3>Organización en capas (Clean Architecture)</h3>
<p>Las dependencias apuntan siempre hacia el centro: Domain no depende de nada, Application define las interfaces e Infrastructure las implementa.</p>
<figure><img src="{img(D/'capas.png')}" style="max-width:62%"><figcaption>Figura B. Capas y dirección de las dependencias.</figcaption></figure>

<h2 class="salto">3. Atributos de calidad</h2>
{tabla(["Atributo","Cómo se garantiza","Dónde"],[
 ["<b>Rendimiento</b>","Las fuentes se extraen en paralelo (<code>Task.WhenAll</code>). Las lecturas son asíncronas y en streaming (<code>IAsyncEnumerable</code>), sin cargar todo en memoria. La escritura usa <code>SqlBulkCopy</code> por lotes configurables. El tiempo de cada fuente se mide con <code>Stopwatch</code>.","ExtraccionOrquestador, StagingRepository"],
 ["<b>Escalabilidad</b>","Agregar una fuente nueva solo requiere una clase que implemente <code>IExtractor</code>, su registro en una línea de DI y su sección en <code>appsettings.json</code>. Cada fuente se puede habilitar o deshabilitar con <code>Habilitada</code>. El API se consume por páginas.","IExtractor, DependencyInjection, appsettings.json"],
 ["<b>Seguridad</b>","Ningún secreto va en el código ni en el repositorio: la clave del API se guarda en <i>user-secrets</i> o variable de entorno. Las consultas usan parámetros y la consulta de extracción es fija, sin concatenar entradas. La clave se envía en la cabecera <code>X-Api-Key</code>. <code>appsettings.Local.json</code> y los logs están en .gitignore.","appsettings.json, ApiExtractor, StagingRepository, .gitignore"],
 ["<b>Mantenibilidad</b>","Separación en cuatro proyectos por capa, interfaces para las fuentes y el repositorio (inyección de dependencias), una clase por responsabilidad (SOLID) y configuración tipada con el patrón Options. Logs estructurados con ILogger y Serilog.","Solución completa"]])}

<h3>Tolerancia a fallos</h3>
<ul>
<li>Si una fuente falla, las demás continúan; el error queda en el log y en <code>stg.LogExtraccion</code> con estado ERROR.</li>
<li>El API aplica reintentos con espera exponencial (2, 4 y 8 segundos) solo ante errores transitorios (red, timeout o HTTP 5xx). Un 401 no se reintenta.</li>
<li>Los registros inválidos se rechazan con su motivo en el log y no detienen el proceso.</li>
</ul>

<h2 class="salto">4. Flujo del proceso ETL</h2>
<figure><img src="{img(D/'flujo_etl.png')}" style="max-height:200mm"><figcaption>Figura C. Diagrama de flujo de la extracción.</figcaption></figure>
""")

partes.append(f"""
<h2 class="salto">5. Justificación de decisiones técnicas</h2>
{tabla(["Decisión","Justificación"],[
 ["Worker Service en lugar de aplicación de consola","Tiene ciclo de vida, inyección de dependencias, configuración y logging integrados, y puede ejecutarse como servicio de Windows o en contenedor."],
 ["Clean Architecture","Aísla las reglas del ETL de los detalles (SQL Server, HTTP, CSV). Permite cambiar una fuente o probar el orquestador sin tocar el resto."],
 ["IExtractor con tres implementaciones","Cumple el principio abierto/cerrado: se agregan fuentes sin modificar el orquestador."],
 ["Validación centralizada (ValidadorRegistros)","Las mismas reglas de calidad aplican a todas las fuentes, y los extractores quedan simples."],
 ["Staging en la base analítica (esquema stg)","Separa los datos crudos de las tablas del modelo y permite auditar, reprocesar y cargar al modelo después (la T y la L)."],
 ["SqlBulkCopy por lotes","Es el método más rápido para insertar grandes volúmenes en SQL Server y mantiene la memoria acotada."],
 ["CsvHelper","Maneja comillas, comas dentro de texto y codificación, en lugar de un parser propio."],
 ["IHttpClientFactory","Gestiona el pool de conexiones y evita el agotamiento de sockets; centraliza URL base, timeout y cabeceras."],
 ["Serilog junto a ILogger","El código depende solo de ILogger; Serilog agrega salida a consola y a archivo diario sin cambiar las clases."],
 ["API simulada local (Etl.MockApi)","Permite probar el consumo de un API REST con paginación y autenticación sin depender de internet ni de terceros."],
 ["BD fuente OpinionesExterno","Representa un sistema externo (tienda en línea) con su propio esquema, distinto del analítico."]])}

<h2 class="salto">6. Evidencia del código</h2>
<h3>6.1 Contrato de extracción</h3>
{codigo("src/Etl.Application/Abstracciones/IExtractor.cs")}
<h3>6.2 Extractor CSV</h3>
{codigo("src/Etl.Infrastructure/Extractores/CsvExtractor.cs", 34, 69)}
<h3>6.3 Extractor de base de datos</h3>
{codigo("src/Etl.Infrastructure/Extractores/DatabaseExtractor.cs", 13, 48)}
<h3>6.4 Extractor de API REST</h3>
{codigo("src/Etl.Infrastructure/Extractores/ApiExtractor.cs", 51, 105)}
<h3>6.5 Orquestador (paralelismo, lotes, logs y métricas)</h3>
{codigo("src/Etl.Application/Servicios/ExtraccionOrquestador.cs", 29, 116)}
<h3>6.6 Carga a staging</h3>
{codigo("src/Etl.Infrastructure/Persistencia/StagingRepository.cs", 25, 66)}
<h3>6.7 Configuración modular</h3>
{codigo("src/Etl.Worker/appsettings.json")}
<h3>6.8 Registro de fuentes y composición</h3>
{codigo("src/Etl.Infrastructure/DependencyInjection.cs", 13, 36)}
{codigo("src/Etl.Worker/Program.cs")}
""")

esp = tabla(["Fuente","Leídos","Válidos","Rechazados","Motivo de los rechazos (datos de prueba)"],[
 ["CSV","90","84","6","2 comentarios vacíos, 1 calificación 9, 1 calificación no numérica, 1 fecha inválida, 1 correo inválido"],
 ["BASE_DATOS","240","235","5","3 comentarios vacíos, 2 calificaciones en 0"],
 ["API_REST","150","145","5","3 comentarios vacíos, 2 calificaciones en 7"],
 ["<b>Total</b>","<b>480</b>","<b>464</b>","<b>16</b>",""]])

partes.append(f"""
<h2 class="salto">7. Evidencia de ejecución</h2>
<p>Los datos de prueba incluyen filas defectuosas a propósito para comprobar la validación. Resultado esperado:</p>
{esp}
{captura(1,"Mock API en ejecución","API REST simulada escuchando en http://localhost:5099.")}
{captura(2,"Worker Service ejecutándose en consola","Ejecución del Worker: logs por fuente, advertencias de registros rechazados y métricas de tiempo.")}
{captura(3,"Resumen por origen y bitácora en SQL Server","Consulta a stg.vw_ResumenPorOrigen y stg.LogExtraccion.")}
{captura(4,"Muestra de datos en stg.OpinionesExtraidas","Registros extraídos en la tabla de staging.")}
{captura(5,"Estructura de la solución en Visual Studio","Proyectos por capa en el Explorador de soluciones.", opcional=True)}

<h2>8. Conclusión</h2>
<p>La solución extrae tres tipos de fuente en paralelo hacia staging, con validación, bitácora y métricas, y cumple los atributos de calidad pedidos. Queda preparada para las siguientes etapas del ETL: transformar los datos de staging y cargarlos al modelo analítico, y alimentar el dashboard.</p>
""")

doc = f"<!doctype html><html lang='es'><head><meta charset='utf-8'><style>{CSS}</style></head><body>{''.join(partes)}</body></html>"
salida = DOCS / "Documento_Tecnico_ETL.pdf"
(DOCS / "_doc.html").write_text(doc, encoding="utf-8")
with sync_playwright() as p:
    b = p.chromium.launch()
    pg = b.new_page()
    pg.goto((DOCS / "_doc.html").as_uri())
    pg.pdf(path=str(salida), format="Letter", print_background=True,
           margin={"top":"18mm","bottom":"18mm","left":"16mm","right":"16mm"},
           display_header_footer=False)
    b.close()
(DOCS / "_doc.html").unlink()
print("PDF:", salida)
