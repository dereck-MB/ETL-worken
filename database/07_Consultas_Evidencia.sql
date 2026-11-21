USE SistemaAnalisisOpiniones;
GO

-- 1. Registros extraidos por origen
SELECT * FROM stg.vw_ResumenPorOrigen;

-- 2. Bitacora de la ultima ejecucion
SELECT TOP 3 Origen, Leidos, Validos, Rechazados, DuracionMs, Estado, Fecha
FROM stg.LogExtraccion
ORDER BY LogID DESC;

-- 3. Muestra de datos en staging
SELECT TOP 10 StagingID, Origen, IdExterno, Cliente, Producto, Calificacion, Comentario, FechaOrigen
FROM stg.OpinionesExtraidas
ORDER BY StagingID;

-- 4. Total general
SELECT COUNT(*) AS TotalEnStaging FROM stg.OpinionesExtraidas;
GO
