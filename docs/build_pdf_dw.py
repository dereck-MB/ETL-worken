import base64, html
from pathlib import Path
from playwright.sync_api import sync_playwright

RAIZ = Path(__file__).resolve().parent.parent
DOCS = RAIZ / "docs"
D = DOCS / "diagramas"
REPO = "https://github.com/dereck-MB/ETL-worken"

def img(p):
    return "data:image/png;base64," + base64.b64encode(Path(p).read_bytes()).decode()

def codigo(ruta, ini=None, fin=None):
    lineas = (RAIZ / ruta).read_text(encoding="utf-8").splitlines()
    if ini: lineas = lineas[ini - 1: fin]
    return f'<pre>{html.escape(chr(10).join(lineas))}</pre>'

def captura(n, titulo):
    f = DOCS / "capturas_dw" / f"captura_{n}.png"
    if f.exists():
        cuerpo = f'<img class="cap" src="{img(f)}">'
    else:
        cuerpo = f'<div class="pend">Captura pendiente: {html.escape(titulo)}<br><small>Guardar como docs/capturas_dw/captura_{n}.png</small></div>'
    return f'<figure>{cuerpo}</figure>'

def tabla(cab, filas):
    h = "".join(f"<th>{c}</th>" for c in cab)
    b = "".join("<tr>" + "".join(f"<td>{c}</td>" for c in f) + "</tr>" for f in filas)
    return f'<table class="t"><tr>{h}</tr>{b}</table>'

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

P = []
P.append(f"""
<h1>Carga de dimensiones al DataWarehouse</h1>
<div class="sub">Sistema de Análisis de Opiniones de Clientes — Proceso ETL en .NET 8</div>

<h2>1. Objetivo</h2>
<p>Cargar las <b>dimensiones</b> de la base de datos <b>DataWarehouse</b> con un proceso ETL desarrollado en .NET 8. El proceso extrae los datos maestros de la base relacional <b>SistemaAnalisisOpiniones</b> y las opiniones ya extraídas en staging (actividad anterior), los transforma y los carga en un modelo dimensional. Se amplió el mismo Worker Service: las fuentes de la actividad anterior (CSV, base de datos y API) alimentan el staging, y ahora el Worker carga desde ahí las dimensiones.</p>
<p>Repositorio en GitHub: <b>{REPO}</b></p>

<h2>2. Modelo dimensional</h2>
<p>Se creó la base <code>DataWarehouse</code> con dos esquemas: <code>dw</code> para las dimensiones y <code>etl</code> para las tablas de paso, los procedimientos de carga y la bitácora. Cada dimensión tiene una llave sustituta (<code>*Key</code>), una clave natural (<code>*NK</code>) para identificar el registro de origen, y un miembro «desconocido» para que las tablas de hechos futuras nunca tengan llaves nulas.</p>
<figure><img src="{img(D/'dw_modelo.png')}"></figure>
{tabla(["Dimensión","Origen de los datos","Clave natural","Atributos principales"],[
 ["DimTiempo","Generada en el proceso (rango de años configurable, 2025 a 2027)","Fecha (TiempoKey = AAAAMMDD)","Año, trimestre, mes, nombre del mes, día, día de la semana, semana ISO, fin de semana"],
 ["DimCliente","dbo.Clientes y correos de stg.OpinionesExtraidas","Correo (o SINCORREO-id si no tiene)","Nombre, país, edad, rango de edad, tipo de cliente, fecha de registro, origen del dato"],
 ["DimProducto","dbo.Productos y productos de staging","Nombre normalizado","Descripción, categoría, precio, rango de precio, origen del dato"],
 ["DimFuente","dbo.Fuentes y canales de staging","Nombre normalizado","Tipo de canal, origen del dato"],
 ["DimSentimiento","Catálogo definido en el proceso","SentimientoKey","Sentimiento, rango de calificación, descripción"],
 ["DimEvento","dbo.Eventos","EventoID de origen","Nombre, tipo, producto (llave a DimProducto), fechas y duración en días"]])}

<h2 class="salto">3. Descripción del proceso</h2>
<figure><img src="{img(D/'dw_arquitectura.png')}"></figure>
<h3>Extracción</h3>
<p>Cada dimensión tiene su cargador (<code>IDimensionLoader</code>) que lee la tabla maestra de la base relacional con <code>SqlDataReader</code>. Cuando está habilitado <code>IncluirStaging</code>, también toma los valores distintos de <code>stg.OpinionesExtraidas</code> (correos, productos y canales), de modo que los datos de las tres fuentes de la actividad anterior quedan integrados en las dimensiones.</p>
<h3>Transformación</h3>
{tabla(["Regla","Detalle"],[
 ["Limpieza","Se recortan espacios y se unifican los espacios múltiples."],
 ["Clave natural","Se normaliza a minúsculas para no duplicar valores como «Instagram» e «instagram». El correo se valida y se guarda en minúsculas."],
 ["Valores nulos","Los atributos sin dato se completan con «Sin definir»."],
 ["Rango de edad","Menor de 18, 18-25, 26-35, 36-50 y 51 o más."],
 ["Rango de precio","Económico (menos de 500), Medio (500 a 1,999) y Premium (2,000 o más)."],
 ["Tipo de canal","Para canales nuevos: Instagram, Facebook, Twitter/X y TikTok son Red Social; Google es Reseña Pública; WhatsApp es Mensajería; web y tienda son Web; las encuestas del CSV son Encuesta Interna."],
 ["Integración de fuentes","Si el registro existe en la base relacional, esta manda; si solo viene de staging se marca STAGING; si está en ambas, AMBOS."],
 ["Evento","Duración en días = fecha fin − fecha inicio + 1. El producto se resuelve a ProductoKey y, si no tiene, usa el miembro desconocido."]])}
<h3>Carga</h3>
<p>Por cada dimensión se abre una transacción, se vacía la tabla de paso <code>etl.Stg_Dim*</code>, se carga con <code>SqlBulkCopy</code> y se ejecuta el procedimiento <code>etl.sp_Cargar_Dim*</code>, que aplica un <code>MERGE</code>: inserta los registros nuevos y actualiza solo los que cambiaron. Si algo falla se hace rollback de esa dimensión y el error queda en la bitácora <code>etl.LogCarga</code>.</p>
<figure><img src="{img(D/'dw_flujo.png')}" style="max-height:225mm"></figure>

<h2 class="salto">4. Decisiones técnicas</h2>
{tabla(["Decisión","Justificación"],[
 ["MERGE con detección de cambios","El proceso es idempotente: se puede ejecutar varias veces sin duplicar filas. Solo actualiza lo que realmente cambió."],
 ["Dimensiones tipo 1 (SCD 1)","Se sobrescribe el valor anterior. Es suficiente para este análisis y mantiene el modelo simple; las columnas FechaCarga y FechaActualizacion dejan trazabilidad."],
 ["Tablas de paso etl.Stg_Dim*","Separan la extracción de la carga final y permiten usar un MERGE por conjuntos en lugar de insertar fila por fila."],
 ["Transacción por dimensión","Una dimensión queda cargada completa o no se carga, sin estados a medias."],
 ["Miembros desconocidos (-1 y 0)","Evitan llaves nulas cuando una opinión no tiene cliente, producto, fuente, evento o fecha."],
 ["Orden de carga por dependencia","DimProducto se carga antes que DimEvento porque este la referencia."],
 ["Columna OrigenDato","Indica si el registro viene de la base relacional, del staging o de ambos, y permite auditar la integración."],
 ["Configuración en appsettings.json","Cadenas de conexión, rango de años de DimTiempo, uso del staging y modo de ejecución (Extraccion, Dimensiones o Todo) se cambian sin recompilar."],
 ["Un cargador por dimensión","Agregar una dimensión nueva solo requiere una clase, un procedimiento MERGE y una línea de registro en la inyección de dependencias."]])}

<h2 class="salto">5. Evidencia del código</h2>
<h3>5.1 Cargador base: transacción, paso, carga masiva y MERGE</h3>
{codigo("src/Etl.Infrastructure/Dimensiones/DimensionLoaderBase.cs", 24, 70)}
<h3>5.2 Extracción y transformación de DimCliente</h3>
{codigo("src/Etl.Infrastructure/Dimensiones/DimClienteLoader.cs", 27, 86)}
<h3>5.3 Procedimiento de carga con MERGE</h3>
{codigo("database/08_DataWarehouse.sql", 227, 250)}
<h3>5.4 Orquestador de la carga</h3>
{codigo("src/Etl.Application/Servicios/CargaDimensionesOrquestador.cs", 28, 78)}
""")

P.append(f"""
<h2 class="salto">6. Evidencia de funcionamiento</h2>
<p>Ejecución del Worker en modo <code>Dimensiones</code>: carga las seis dimensiones y muestra, por cada una, los registros extraídos, insertados, actualizados y sin cambio.</p>
{captura(1, "Consola del Worker, primera ejecución")}
<p>Resumen de filas por dimensión y bitácora de la carga en SQL Server.</p>
{captura(2, "Resumen por dimensión y bitácora (etl.LogCarga)")}

<h2 class="salto">7. Tablas cargadas</h2>
<p>Clientes y productos cargados en el DataWarehouse. La columna OrigenDato muestra si el registro viene de la base relacional (OLTP), del staging (STAGING) o de ambos (AMBOS).</p>
{captura(3, "DimCliente y DimProducto")}
<p>Fuentes, sentimientos y eventos.</p>
{captura(4, "DimFuente, DimSentimiento y DimEvento")}
<p>Dimensión de tiempo y distribución del origen de los miembros.</p>
{captura(5, "DimTiempo y origen de los registros")}

<h2>8. Prueba de idempotencia</h2>
<p>Al ejecutar el proceso por segunda vez, sin cambios en el origen, no se insertan ni actualizan filas: todo aparece como «sin cambio». Esto confirma que el MERGE no duplica datos.</p>
{captura(6, "Consola del Worker, segunda ejecución (0 insertados)")}

<h2>9. Repositorio</h2>
<p>El código fuente completo (Worker, scripts SQL, diagramas y documentación) está en GitHub:</p>
<p><b>{REPO}</b></p>
""")

doc = f"<!doctype html><html lang='es'><head><meta charset='utf-8'><style>{CSS}</style></head><body>{''.join(P)}</body></html>"
salida = DOCS / "Documento_Carga_Dimensiones.pdf"
(DOCS / "_doc_dw.html").write_text(doc, encoding="utf-8")
with sync_playwright() as p:
    b = p.chromium.launch()
    pg = b.new_page()
    pg.goto((DOCS / "_doc_dw.html").as_uri())
    pg.pdf(path=str(salida), format="Letter", print_background=True,
           margin={"top":"18mm","bottom":"18mm","left":"16mm","right":"16mm"},
           display_header_footer=False)
    b.close()
(DOCS / "_doc_dw.html").unlink()
print("PDF:", salida)
