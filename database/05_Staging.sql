USE SistemaAnalisisOpiniones;
GO

IF SCHEMA_ID('stg') IS NULL EXEC('CREATE SCHEMA stg');
GO

IF OBJECT_ID('stg.OpinionesExtraidas','U') IS NULL
CREATE TABLE stg.OpinionesExtraidas (
    StagingID       BIGINT IDENTITY(1,1) PRIMARY KEY,
    LoteID          UNIQUEIDENTIFIER NOT NULL,
    Origen          VARCHAR(20)   NOT NULL,
    IdExterno       VARCHAR(50)   NOT NULL,
    Cliente         VARCHAR(150)  NULL,
    Correo          VARCHAR(150)  NULL,
    Producto        VARCHAR(150)  NULL,
    Canal           VARCHAR(100)  NULL,
    Calificacion    TINYINT       NULL,
    Comentario      VARCHAR(1000) NOT NULL,
    FechaOrigen     DATE          NULL,
    FechaExtraccion DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
GO

IF OBJECT_ID('stg.LogExtraccion','U') IS NULL
CREATE TABLE stg.LogExtraccion (
    LogID        INT IDENTITY(1,1) PRIMARY KEY,
    LoteID       UNIQUEIDENTIFIER NOT NULL,
    Origen       VARCHAR(20)   NOT NULL,
    Leidos       INT           NOT NULL,
    Validos      INT           NOT NULL,
    Rechazados   INT           NOT NULL,
    DuracionMs   BIGINT        NOT NULL,
    Estado       VARCHAR(15)   NOT NULL,
    Mensaje      VARCHAR(500)  NULL,
    Fecha        DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE OR ALTER VIEW stg.vw_ResumenPorOrigen AS
SELECT Origen,
       COUNT(*)                         AS Registros,
       AVG(CAST(Calificacion AS FLOAT)) AS CalificacionPromedio,
       MIN(FechaOrigen)                 AS FechaMin,
       MAX(FechaOrigen)                 AS FechaMax
FROM stg.OpinionesExtraidas
GROUP BY Origen;
GO

CREATE OR ALTER PROCEDURE stg.sp_LimpiarStaging
AS
BEGIN
    SET NOCOUNT ON;
    TRUNCATE TABLE stg.OpinionesExtraidas;
END;
GO
