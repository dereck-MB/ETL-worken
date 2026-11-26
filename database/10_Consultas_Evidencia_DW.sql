USE DataWarehouse;
GO

-- CAPTURA 2: filas por dimension y bitacora de la ultima carga
SELECT * FROM dw.vw_ResumenDimensiones;

SELECT TOP 6 Dimension, Extraidos, Insertados, Actualizados, SinCambio, DuracionMs, Estado, Fecha
FROM etl.LogCarga ORDER BY LogID DESC;
GO

-- CAPTURA 3: DimCliente y DimProducto
SELECT TOP 10 ClienteKey, ClienteNK, Nombre, Correo, Pais, Edad, RangoEdad, TipoCliente, OrigenDato
FROM dw.DimCliente ORDER BY ClienteKey;

SELECT TOP 10 ProductoKey, ProductoNK, Nombre, Categoria, Precio, RangoPrecio, OrigenDato
FROM dw.DimProducto ORDER BY ProductoKey;
GO

-- CAPTURA 4: DimFuente, DimSentimiento y DimEvento
SELECT FuenteKey, FuenteNK, Nombre, TipoCanal, OrigenDato FROM dw.DimFuente ORDER BY FuenteKey;

SELECT SentimientoKey, Sentimiento, RangoCalificacion, Descripcion FROM dw.DimSentimiento ORDER BY SentimientoKey;

SELECT EventoKey, EventoIDOrigen, Nombre, Tipo, ProductoKey, FechaInicio, FechaFin, DuracionDias
FROM dw.DimEvento ORDER BY EventoKey;
GO

-- CAPTURA 5: DimTiempo y origen de los miembros
SELECT TOP 10 TiempoKey, Fecha, Anio, Trimestre, Mes, NombreMes, Dia, NombreDia, SemanaAnio, EsFinDeSemana
FROM dw.DimTiempo WHERE TiempoKey >= 20260101 ORDER BY TiempoKey;

SELECT 'DimCliente' AS Dimension, OrigenDato, COUNT(*) AS Filas FROM dw.DimCliente GROUP BY OrigenDato
UNION ALL SELECT 'DimProducto', OrigenDato, COUNT(*) FROM dw.DimProducto GROUP BY OrigenDato
UNION ALL SELECT 'DimFuente',   OrigenDato, COUNT(*) FROM dw.DimFuente   GROUP BY OrigenDato;
GO
