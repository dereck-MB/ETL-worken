-- Base DataWarehouse: dimensiones, tablas de paso (etl), procedimientos de carga y bitacora.
-- Idempotente: se puede ejecutar varias veces.
IF DB_ID('DataWarehouse') IS NULL
    CREATE DATABASE DataWarehouse;
GO

USE DataWarehouse;
GO

IF SCHEMA_ID('dw') IS NULL EXEC('CREATE SCHEMA dw');
GO
IF SCHEMA_ID('etl') IS NULL EXEC('CREATE SCHEMA etl');
GO

/* ===================== DIMENSIONES ===================== */

IF OBJECT_ID('dw.DimTiempo','U') IS NULL
CREATE TABLE dw.DimTiempo (
    TiempoKey     INT         NOT NULL CONSTRAINT PK_DimTiempo PRIMARY KEY,
    Fecha         DATE        NOT NULL CONSTRAINT UQ_DimTiempo_Fecha UNIQUE,
    Anio          SMALLINT    NOT NULL,
    Trimestre     TINYINT     NOT NULL,
    Mes           TINYINT     NOT NULL,
    NombreMes     VARCHAR(15) NOT NULL,
    Dia           TINYINT     NOT NULL,
    DiaSemana     TINYINT     NOT NULL,
    NombreDia     VARCHAR(15) NOT NULL,
    SemanaAnio    TINYINT     NOT NULL,
    EsFinDeSemana BIT         NOT NULL
);
GO

IF OBJECT_ID('dw.DimCliente','U') IS NULL
CREATE TABLE dw.DimCliente (
    ClienteKey         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DimCliente PRIMARY KEY,
    ClienteNK          VARCHAR(150) NOT NULL CONSTRAINT UQ_DimCliente_NK UNIQUE,
    ClienteIDOrigen    INT          NULL,
    Nombre             VARCHAR(150) NOT NULL,
    Correo             VARCHAR(150) NULL,
    Pais               VARCHAR(60)  NOT NULL,
    Edad               TINYINT      NULL,
    RangoEdad          VARCHAR(20)  NOT NULL,
    TipoCliente        VARCHAR(30)  NOT NULL,
    FechaRegistro      DATE         NULL,
    OrigenDato         VARCHAR(10)  NOT NULL,
    FechaCarga         DATETIME2    NOT NULL CONSTRAINT DF_DimCliente_Carga DEFAULT SYSDATETIME(),
    FechaActualizacion DATETIME2    NULL
);
GO

IF OBJECT_ID('dw.DimProducto','U') IS NULL
CREATE TABLE dw.DimProducto (
    ProductoKey        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DimProducto PRIMARY KEY,
    ProductoNK         VARCHAR(150) NOT NULL CONSTRAINT UQ_DimProducto_NK UNIQUE,
    ProductoIDOrigen   INT          NULL,
    Nombre             VARCHAR(150) NOT NULL,
    Descripcion        VARCHAR(250) NULL,
    Categoria          VARCHAR(100) NOT NULL,
    Precio             DECIMAL(10,2) NULL,
    RangoPrecio        VARCHAR(20)  NOT NULL,
    OrigenDato         VARCHAR(10)  NOT NULL,
    FechaCarga         DATETIME2    NOT NULL CONSTRAINT DF_DimProducto_Carga DEFAULT SYSDATETIME(),
    FechaActualizacion DATETIME2    NULL
);
GO

IF OBJECT_ID('dw.DimFuente','U') IS NULL
CREATE TABLE dw.DimFuente (
    FuenteKey          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DimFuente PRIMARY KEY,
    FuenteNK           VARCHAR(100) NOT NULL CONSTRAINT UQ_DimFuente_NK UNIQUE,
    FuenteIDOrigen     INT          NULL,
    Nombre             VARCHAR(100) NOT NULL,
    TipoCanal          VARCHAR(50)  NOT NULL,
    OrigenDato         VARCHAR(10)  NOT NULL,
    FechaCarga         DATETIME2    NOT NULL CONSTRAINT DF_DimFuente_Carga DEFAULT SYSDATETIME(),
    FechaActualizacion DATETIME2    NULL
);
GO

IF OBJECT_ID('dw.DimSentimiento','U') IS NULL
CREATE TABLE dw.DimSentimiento (
    SentimientoKey     TINYINT      NOT NULL CONSTRAINT PK_DimSentimiento PRIMARY KEY,
    Sentimiento        VARCHAR(20)  NOT NULL CONSTRAINT UQ_DimSentimiento UNIQUE,
    RangoCalificacion  VARCHAR(10)  NOT NULL,
    Descripcion        VARCHAR(100) NOT NULL,
    FechaCarga         DATETIME2    NOT NULL CONSTRAINT DF_DimSentimiento_Carga DEFAULT SYSDATETIME(),
    FechaActualizacion DATETIME2    NULL
);
GO

IF OBJECT_ID('dw.DimEvento','U') IS NULL
CREATE TABLE dw.DimEvento (
    EventoKey          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DimEvento PRIMARY KEY,
    EventoIDOrigen     INT          NOT NULL CONSTRAINT UQ_DimEvento_ID UNIQUE,
    Nombre             VARCHAR(150) NOT NULL,
    Tipo               VARCHAR(50)  NOT NULL,
    ProductoKey        INT          NOT NULL CONSTRAINT FK_DimEvento_Producto REFERENCES dw.DimProducto(ProductoKey),
    FechaInicio        DATE         NULL,
    FechaFin           DATE         NULL,
    DuracionDias       INT          NULL,
    FechaCarga         DATETIME2    NOT NULL CONSTRAINT DF_DimEvento_Carga DEFAULT SYSDATETIME(),
    FechaActualizacion DATETIME2    NULL
);
GO

/* ===================== MIEMBROS DESCONOCIDOS ===================== */

IF NOT EXISTS (SELECT 1 FROM dw.DimTiempo WHERE TiempoKey = 0)
    INSERT INTO dw.DimTiempo (TiempoKey, Fecha, Anio, Trimestre, Mes, NombreMes, Dia, DiaSemana, NombreDia, SemanaAnio, EsFinDeSemana)
    VALUES (0, '1900-01-01', 1900, 1, 1, 'Sin fecha', 1, 1, 'Sin fecha', 1, 0);
GO

IF NOT EXISTS (SELECT 1 FROM dw.DimCliente WHERE ClienteKey = -1)
BEGIN
    SET IDENTITY_INSERT dw.DimCliente ON;
    INSERT INTO dw.DimCliente (ClienteKey, ClienteNK, Nombre, Pais, RangoEdad, TipoCliente, OrigenDato)
    VALUES (-1, 'DESCONOCIDO', 'Cliente desconocido', 'Sin definir', 'Sin definir', 'Sin definir', 'SISTEMA');
    SET IDENTITY_INSERT dw.DimCliente OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dw.DimProducto WHERE ProductoKey = -1)
BEGIN
    SET IDENTITY_INSERT dw.DimProducto ON;
    INSERT INTO dw.DimProducto (ProductoKey, ProductoNK, Nombre, Categoria, RangoPrecio, OrigenDato)
    VALUES (-1, 'DESCONOCIDO', 'Producto desconocido', 'Sin definir', 'Sin definir', 'SISTEMA');
    SET IDENTITY_INSERT dw.DimProducto OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dw.DimFuente WHERE FuenteKey = -1)
BEGIN
    SET IDENTITY_INSERT dw.DimFuente ON;
    INSERT INTO dw.DimFuente (FuenteKey, FuenteNK, Nombre, TipoCanal, OrigenDato)
    VALUES (-1, 'DESCONOCIDO', 'Fuente desconocida', 'Sin definir', 'SISTEMA');
    SET IDENTITY_INSERT dw.DimFuente OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dw.DimEvento WHERE EventoKey = -1)
BEGIN
    SET IDENTITY_INSERT dw.DimEvento ON;
    INSERT INTO dw.DimEvento (EventoKey, EventoIDOrigen, Nombre, Tipo, ProductoKey)
    VALUES (-1, -1, 'Sin evento', 'Sin definir', -1);
    SET IDENTITY_INSERT dw.DimEvento OFF;
END;
GO

/* ===================== TABLAS DE PASO (STAGING DEL DW) ===================== */

IF OBJECT_ID('etl.Stg_DimTiempo','U') IS NULL
CREATE TABLE etl.Stg_DimTiempo (
    TiempoKey INT NOT NULL, Fecha DATE NOT NULL, Anio SMALLINT NOT NULL, Trimestre TINYINT NOT NULL,
    Mes TINYINT NOT NULL, NombreMes VARCHAR(15) NOT NULL, Dia TINYINT NOT NULL, DiaSemana TINYINT NOT NULL,
    NombreDia VARCHAR(15) NOT NULL, SemanaAnio TINYINT NOT NULL, EsFinDeSemana BIT NOT NULL
);
GO
IF OBJECT_ID('etl.Stg_DimCliente','U') IS NULL
CREATE TABLE etl.Stg_DimCliente (
    ClienteNK VARCHAR(150) NOT NULL, ClienteIDOrigen INT NULL, Nombre VARCHAR(150) NOT NULL, Correo VARCHAR(150) NULL,
    Pais VARCHAR(60) NOT NULL, Edad TINYINT NULL, RangoEdad VARCHAR(20) NOT NULL, TipoCliente VARCHAR(30) NOT NULL,
    FechaRegistro DATE NULL, OrigenDato VARCHAR(10) NOT NULL
);
GO
IF OBJECT_ID('etl.Stg_DimProducto','U') IS NULL
CREATE TABLE etl.Stg_DimProducto (
    ProductoNK VARCHAR(150) NOT NULL, ProductoIDOrigen INT NULL, Nombre VARCHAR(150) NOT NULL, Descripcion VARCHAR(250) NULL,
    Categoria VARCHAR(100) NOT NULL, Precio DECIMAL(10,2) NULL, RangoPrecio VARCHAR(20) NOT NULL, OrigenDato VARCHAR(10) NOT NULL
);
GO
IF OBJECT_ID('etl.Stg_DimFuente','U') IS NULL
CREATE TABLE etl.Stg_DimFuente (
    FuenteNK VARCHAR(100) NOT NULL, FuenteIDOrigen INT NULL, Nombre VARCHAR(100) NOT NULL,
    TipoCanal VARCHAR(50) NOT NULL, OrigenDato VARCHAR(10) NOT NULL
);
GO
IF OBJECT_ID('etl.Stg_DimSentimiento','U') IS NULL
CREATE TABLE etl.Stg_DimSentimiento (
    SentimientoKey TINYINT NOT NULL, Sentimiento VARCHAR(20) NOT NULL, RangoCalificacion VARCHAR(10) NOT NULL, Descripcion VARCHAR(100) NOT NULL
);
GO
IF OBJECT_ID('etl.Stg_DimEvento','U') IS NULL
CREATE TABLE etl.Stg_DimEvento (
    EventoIDOrigen INT NOT NULL, Nombre VARCHAR(150) NOT NULL, Tipo VARCHAR(50) NOT NULL, ProductoIDOrigen INT NULL,
    FechaInicio DATE NULL, FechaFin DATE NULL, DuracionDias INT NULL
);
GO

/* ===================== BITACORA ===================== */

IF OBJECT_ID('etl.LogCarga','U') IS NULL
CREATE TABLE etl.LogCarga (
    LogID        INT IDENTITY(1,1) PRIMARY KEY,
    LoteID       UNIQUEIDENTIFIER NOT NULL,
    Dimension    VARCHAR(30)  NOT NULL,
    Extraidos    INT          NOT NULL,
    Insertados   INT          NOT NULL,
    Actualizados INT          NOT NULL,
    SinCambio    INT          NOT NULL,
    DuracionMs   BIGINT       NOT NULL,
    Estado       VARCHAR(10)  NOT NULL,
    Mensaje      VARCHAR(500) NULL,
    Fecha        DATETIME2    NOT NULL DEFAULT SYSDATETIME()
);
GO

/* ===================== PROCEDIMIENTOS DE CARGA (MERGE) ===================== */

CREATE OR ALTER PROCEDURE etl.sp_Cargar_DimTiempo @Insertados INT OUTPUT, @Actualizados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @acc TABLE (Accion NVARCHAR(10));

    MERGE dw.DimTiempo AS d
    USING etl.Stg_DimTiempo AS s ON d.TiempoKey = s.TiempoKey
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (TiempoKey, Fecha, Anio, Trimestre, Mes, NombreMes, Dia, DiaSemana, NombreDia, SemanaAnio, EsFinDeSemana)
        VALUES (s.TiempoKey, s.Fecha, s.Anio, s.Trimestre, s.Mes, s.NombreMes, s.Dia, s.DiaSemana, s.NombreDia, s.SemanaAnio, s.EsFinDeSemana)
    OUTPUT $action INTO @acc;

    SELECT @Insertados = COUNT(*) FROM @acc WHERE Accion = 'INSERT';
    SET @Actualizados = 0;
END;
GO

CREATE OR ALTER PROCEDURE etl.sp_Cargar_DimCliente @Insertados INT OUTPUT, @Actualizados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @acc TABLE (Accion NVARCHAR(10));

    MERGE dw.DimCliente AS d
    USING etl.Stg_DimCliente AS s ON d.ClienteNK = s.ClienteNK
    WHEN MATCHED AND EXISTS (
        SELECT s.ClienteIDOrigen, s.Nombre, s.Correo, s.Pais, s.Edad, s.RangoEdad, s.TipoCliente, s.FechaRegistro, s.OrigenDato
        EXCEPT
        SELECT d.ClienteIDOrigen, d.Nombre, d.Correo, d.Pais, d.Edad, d.RangoEdad, d.TipoCliente, d.FechaRegistro, d.OrigenDato)
    THEN UPDATE SET
        ClienteIDOrigen = s.ClienteIDOrigen, Nombre = s.Nombre, Correo = s.Correo, Pais = s.Pais, Edad = s.Edad,
        RangoEdad = s.RangoEdad, TipoCliente = s.TipoCliente, FechaRegistro = s.FechaRegistro,
        OrigenDato = s.OrigenDato, FechaActualizacion = SYSDATETIME()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (ClienteNK, ClienteIDOrigen, Nombre, Correo, Pais, Edad, RangoEdad, TipoCliente, FechaRegistro, OrigenDato)
        VALUES (s.ClienteNK, s.ClienteIDOrigen, s.Nombre, s.Correo, s.Pais, s.Edad, s.RangoEdad, s.TipoCliente, s.FechaRegistro, s.OrigenDato)
    OUTPUT $action INTO @acc;

    SELECT @Insertados = COUNT(*) FROM @acc WHERE Accion = 'INSERT';
    SELECT @Actualizados = COUNT(*) FROM @acc WHERE Accion = 'UPDATE';
END;
GO

CREATE OR ALTER PROCEDURE etl.sp_Cargar_DimProducto @Insertados INT OUTPUT, @Actualizados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @acc TABLE (Accion NVARCHAR(10));

    MERGE dw.DimProducto AS d
    USING etl.Stg_DimProducto AS s ON d.ProductoNK = s.ProductoNK
    WHEN MATCHED AND EXISTS (
        SELECT s.ProductoIDOrigen, s.Nombre, s.Descripcion, s.Categoria, s.Precio, s.RangoPrecio, s.OrigenDato
        EXCEPT
        SELECT d.ProductoIDOrigen, d.Nombre, d.Descripcion, d.Categoria, d.Precio, d.RangoPrecio, d.OrigenDato)
    THEN UPDATE SET
        ProductoIDOrigen = s.ProductoIDOrigen, Nombre = s.Nombre, Descripcion = s.Descripcion, Categoria = s.Categoria,
        Precio = s.Precio, RangoPrecio = s.RangoPrecio, OrigenDato = s.OrigenDato, FechaActualizacion = SYSDATETIME()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (ProductoNK, ProductoIDOrigen, Nombre, Descripcion, Categoria, Precio, RangoPrecio, OrigenDato)
        VALUES (s.ProductoNK, s.ProductoIDOrigen, s.Nombre, s.Descripcion, s.Categoria, s.Precio, s.RangoPrecio, s.OrigenDato)
    OUTPUT $action INTO @acc;

    SELECT @Insertados = COUNT(*) FROM @acc WHERE Accion = 'INSERT';
    SELECT @Actualizados = COUNT(*) FROM @acc WHERE Accion = 'UPDATE';
END;
GO

CREATE OR ALTER PROCEDURE etl.sp_Cargar_DimFuente @Insertados INT OUTPUT, @Actualizados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @acc TABLE (Accion NVARCHAR(10));

    MERGE dw.DimFuente AS d
    USING etl.Stg_DimFuente AS s ON d.FuenteNK = s.FuenteNK
    WHEN MATCHED AND EXISTS (
        SELECT s.FuenteIDOrigen, s.Nombre, s.TipoCanal, s.OrigenDato
        EXCEPT
        SELECT d.FuenteIDOrigen, d.Nombre, d.TipoCanal, d.OrigenDato)
    THEN UPDATE SET
        FuenteIDOrigen = s.FuenteIDOrigen, Nombre = s.Nombre, TipoCanal = s.TipoCanal,
        OrigenDato = s.OrigenDato, FechaActualizacion = SYSDATETIME()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (FuenteNK, FuenteIDOrigen, Nombre, TipoCanal, OrigenDato)
        VALUES (s.FuenteNK, s.FuenteIDOrigen, s.Nombre, s.TipoCanal, s.OrigenDato)
    OUTPUT $action INTO @acc;

    SELECT @Insertados = COUNT(*) FROM @acc WHERE Accion = 'INSERT';
    SELECT @Actualizados = COUNT(*) FROM @acc WHERE Accion = 'UPDATE';
END;
GO

CREATE OR ALTER PROCEDURE etl.sp_Cargar_DimSentimiento @Insertados INT OUTPUT, @Actualizados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @acc TABLE (Accion NVARCHAR(10));

    MERGE dw.DimSentimiento AS d
    USING etl.Stg_DimSentimiento AS s ON d.SentimientoKey = s.SentimientoKey
    WHEN MATCHED AND EXISTS (
        SELECT s.Sentimiento, s.RangoCalificacion, s.Descripcion
        EXCEPT
        SELECT d.Sentimiento, d.RangoCalificacion, d.Descripcion)
    THEN UPDATE SET
        Sentimiento = s.Sentimiento, RangoCalificacion = s.RangoCalificacion,
        Descripcion = s.Descripcion, FechaActualizacion = SYSDATETIME()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (SentimientoKey, Sentimiento, RangoCalificacion, Descripcion)
        VALUES (s.SentimientoKey, s.Sentimiento, s.RangoCalificacion, s.Descripcion)
    OUTPUT $action INTO @acc;

    SELECT @Insertados = COUNT(*) FROM @acc WHERE Accion = 'INSERT';
    SELECT @Actualizados = COUNT(*) FROM @acc WHERE Accion = 'UPDATE';
END;
GO

CREATE OR ALTER PROCEDURE etl.sp_Cargar_DimEvento @Insertados INT OUTPUT, @Actualizados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @acc TABLE (Accion NVARCHAR(10));

    MERGE dw.DimEvento AS d
    USING (
        SELECT e.EventoIDOrigen, e.Nombre, e.Tipo, ISNULL(p.ProductoKey, -1) AS ProductoKey,
               e.FechaInicio, e.FechaFin, e.DuracionDias
        FROM etl.Stg_DimEvento AS e
        LEFT JOIN dw.DimProducto AS p ON p.ProductoIDOrigen = e.ProductoIDOrigen
    ) AS s ON d.EventoIDOrigen = s.EventoIDOrigen
    WHEN MATCHED AND EXISTS (
        SELECT s.Nombre, s.Tipo, s.ProductoKey, s.FechaInicio, s.FechaFin, s.DuracionDias
        EXCEPT
        SELECT d.Nombre, d.Tipo, d.ProductoKey, d.FechaInicio, d.FechaFin, d.DuracionDias)
    THEN UPDATE SET
        Nombre = s.Nombre, Tipo = s.Tipo, ProductoKey = s.ProductoKey, FechaInicio = s.FechaInicio,
        FechaFin = s.FechaFin, DuracionDias = s.DuracionDias, FechaActualizacion = SYSDATETIME()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (EventoIDOrigen, Nombre, Tipo, ProductoKey, FechaInicio, FechaFin, DuracionDias)
        VALUES (s.EventoIDOrigen, s.Nombre, s.Tipo, s.ProductoKey, s.FechaInicio, s.FechaFin, s.DuracionDias)
    OUTPUT $action INTO @acc;

    SELECT @Insertados = COUNT(*) FROM @acc WHERE Accion = 'INSERT';
    SELECT @Actualizados = COUNT(*) FROM @acc WHERE Accion = 'UPDATE';
END;
GO

/* ===================== VISTA DE RESUMEN ===================== */

CREATE OR ALTER VIEW dw.vw_ResumenDimensiones AS
SELECT 'DimTiempo' AS Dimension, COUNT(*) AS Filas FROM dw.DimTiempo
UNION ALL SELECT 'DimCliente',     COUNT(*) FROM dw.DimCliente
UNION ALL SELECT 'DimProducto',    COUNT(*) FROM dw.DimProducto
UNION ALL SELECT 'DimFuente',      COUNT(*) FROM dw.DimFuente
UNION ALL SELECT 'DimSentimiento', COUNT(*) FROM dw.DimSentimiento
UNION ALL SELECT 'DimEvento',      COUNT(*) FROM dw.DimEvento;
GO
