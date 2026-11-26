-- Base relacional de origen (SistemaAnalisisOpiniones) con datos de ejemplo ya depurados.
-- Idempotente. Reemplaza la necesidad de correr el proyecto de consola anterior.
IF DB_ID('SistemaAnalisisOpiniones') IS NULL
    CREATE DATABASE SistemaAnalisisOpiniones;
GO

USE SistemaAnalisisOpiniones;
GO


IF OBJECT_ID('dbo.Clientes', 'U') IS NULL
CREATE TABLE Clientes (
    ClienteID INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Correo VARCHAR(150) UNIQUE,
    FechaRegistro DATE NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('dbo.Productos', 'U') IS NULL
CREATE TABLE Productos (
    ProductoID INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(250),
    Categoria VARCHAR(100),
    Precio DECIMAL(10,2)
);
GO

IF OBJECT_ID('dbo.Fuentes', 'U') IS NULL
CREATE TABLE Fuentes (
    FuenteID INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Tipo VARCHAR(50) NOT NULL
);
GO

IF OBJECT_ID('dbo.Opiniones', 'U') IS NULL
CREATE TABLE Opiniones (
    OpinionID INT IDENTITY(1,1) PRIMARY KEY,
    ClienteID INT,
    ProductoID INT,
    FuenteID INT NOT NULL,
    Calificacion INT NOT NULL,
    Comentario VARCHAR(1000) NOT NULL,
    Sentimiento VARCHAR(20),
    FechaOpinion DATE NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Opiniones_Clientes
        FOREIGN KEY (ClienteID) REFERENCES Clientes(ClienteID),

    CONSTRAINT FK_Opiniones_Productos
        FOREIGN KEY (ProductoID) REFERENCES Productos(ProductoID),

    CONSTRAINT FK_Opiniones_Fuentes
        FOREIGN KEY (FuenteID) REFERENCES Fuentes(FuenteID),

    CONSTRAINT CK_Opiniones_Calificacion
        CHECK (Calificacion BETWEEN 1 AND 5),

    CONSTRAINT CK_Opiniones_Sentimiento
        CHECK (Sentimiento IN ('Positivo', 'Neutral', 'Negativo'))
);
GO

IF OBJECT_ID('dbo.Encuestas', 'U') IS NULL
CREATE TABLE Encuestas (
    EncuestaID INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(150) NOT NULL,
    Descripcion VARCHAR(300),
    FechaInicio DATE,
    FechaFin DATE
);
GO

IF OBJECT_ID('dbo.PreguntasEncuesta', 'U') IS NULL
CREATE TABLE PreguntasEncuesta (
    PreguntaID INT IDENTITY(1,1) PRIMARY KEY,
    EncuestaID INT NOT NULL,
    Pregunta VARCHAR(500) NOT NULL,

    CONSTRAINT FK_Preguntas_Encuestas
        FOREIGN KEY (EncuestaID) REFERENCES Encuestas(EncuestaID)
);
GO

IF OBJECT_ID('dbo.RespuestasEncuesta', 'U') IS NULL
CREATE TABLE RespuestasEncuesta (
    RespuestaID INT IDENTITY(1,1) PRIMARY KEY,
    EncuestaID INT NOT NULL,
    PreguntaID INT NOT NULL,
    ClienteID INT NOT NULL,
    Respuesta VARCHAR(500) NOT NULL,
    FechaRespuesta DATE NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Respuestas_Encuestas
        FOREIGN KEY (EncuestaID) REFERENCES Encuestas(EncuestaID),

    CONSTRAINT FK_Respuestas_Preguntas
        FOREIGN KEY (PreguntaID) REFERENCES PreguntasEncuesta(PreguntaID),

    CONSTRAINT FK_Respuestas_Clientes
        FOREIGN KEY (ClienteID) REFERENCES Clientes(ClienteID)
);
GO
GO
IF COL_LENGTH('dbo.Clientes', 'Pais') IS NULL
    ALTER TABLE dbo.Clientes ADD Pais VARCHAR(60) NULL;
IF COL_LENGTH('dbo.Clientes', 'Edad') IS NULL
    ALTER TABLE dbo.Clientes ADD Edad TINYINT NULL;
IF COL_LENGTH('dbo.Clientes', 'TipoCliente') IS NULL
    ALTER TABLE dbo.Clientes ADD TipoCliente VARCHAR(30) NULL;
GO

IF OBJECT_ID('dbo.CK_Clientes_Edad', 'C') IS NULL
    ALTER TABLE dbo.Clientes ADD CONSTRAINT CK_Clientes_Edad CHECK (Edad BETWEEN 0 AND 120);
GO

IF OBJECT_ID('dbo.Eventos', 'U') IS NULL
CREATE TABLE dbo.Eventos (
    EventoID    INT IDENTITY(1,1) PRIMARY KEY,
    Nombre      VARCHAR(150) NOT NULL,
    Tipo        VARCHAR(50)  NULL,
    ProductoID  INT          NULL,
    FechaInicio DATE         NOT NULL,
    FechaFin    DATE         NOT NULL,

    CONSTRAINT FK_Eventos_Productos
        FOREIGN KEY (ProductoID) REFERENCES dbo.Productos(ProductoID),

    CONSTRAINT CK_Eventos_Fechas
        CHECK (FechaFin >= FechaInicio),

    CONSTRAINT UQ_Eventos_NombreInicio
        UNIQUE (Nombre, FechaInicio)
);
GO
GO

/* ===================== DATOS DE EJEMPLO ===================== */
INSERT INTO dbo.Clientes (Nombre, Correo, FechaRegistro, Pais, Edad, TipoCliente)
SELECT v.Nombre, v.Correo, CAST(v.Fecha AS DATE), v.Pais, v.Edad, v.Tipo FROM (VALUES
    ('Sofia Marte', NULL, '2026-02-14', 'Republica Dominicana', 28, 'Regular'),
    ('Nathalie Rosario', 'nathalie.rosario@gmail.com', '2026-01-02', 'Republica Dominicana', 34, 'Regular'),
    ('Carlos Mejia', 'carlos.mejia@gmail.com', '2025-07-23', 'Republica Dominicana', 41, 'Nuevo'),
    ('Yesenia Torres', 'yesenia.torres@gmail.com', '2025-11-30', 'Republica Dominicana', 23, 'Premium'),
    ('Starlin Medina', 'starlin.medina@gmail.com', '2025-11-06', 'Republica Dominicana', 52, 'Premium'),
    ('Carmen Santos', 'carmen.santos@gmail.com', '2026-01-12', 'Mexico', 31, 'Regular'),
    ('Juan Rodriguez', 'juan.rodriguez@gmail.com', '2026-02-18', 'Colombia', 26, 'Nuevo'),
    ('Josefina Ramirez', 'josefina.ramirez@gmail.com', '2026-02-11', 'Espana', 45, 'Regular'),
    ('Maria Perez', 'maria.perez@gmail.com', '2025-07-31', 'Estados Unidos', 38, 'Regular'),
    ('Miguel Reyes', 'miguel.reyes@gmail.com', '2025-11-26', 'Republica Dominicana', 29, 'Regular'),
    ('Bryan Valdez', 'bryan.valdez@gmail.com', '2025-08-06', 'Mexico', 33, 'Premium'),
    ('Altagracia Pena', 'altagracia.pena@gmail.com', '2025-11-22', 'Republica Dominicana', 24, 'Premium'),
    ('Ramon Tavarez', 'ramon.tavarez@hotmail.com', '2026-02-15', 'Republica Dominicana', 48, 'Regular'),
    ('Eduardo Guzman', 'eduardo.guzman@gmail.com', '2026-01-03', 'Republica Dominicana', 36, 'Nuevo'),
    ('Jean Carlos Pimentel', 'jean.carlos.pimentel@gmail.com', '2026-02-01', 'Republica Dominicana', 21, 'Regular'),
    ('Rosa Diaz', 'rosa.diaz@gmail.com', '2025-09-16', 'Republica Dominicana', 43, 'Regular'),
    ('Francisco Nunez', 'francisco.nunez@gmail.com', '2026-02-18', 'Mexico', 30, 'Regular'),
    ('Isabel Cruz', 'isabel.cruz@gmail.com', '2025-06-13', 'Colombia', 27, 'Nuevo'),
    ('Jose Castillo', 'jose.castillo@gmail.com', '2026-02-07', 'Espana', 55, 'Premium'),
    ('Yasmin Polanco', 'yasmin.polanco@gmail.com', '2025-12-06', 'Estados Unidos', 39, 'Premium'),
    ('Massiel Duran', 'massiel.duran@gmail.com', '2026-02-07', 'Colombia', 32, 'Nuevo'),
    ('Rafael Vargas', 'rafael.vargas@gmail.com', '2025-10-28', 'Mexico', 46, 'Regular'),
    ('Wilkin Brito', 'wilkin.brito@gmail.com', '2026-02-24', 'Republica Dominicana', 35, 'Regular'),
    ('Kelvin Santana', 'kelvin.santana@gmail.com', '2026-01-04', 'Republica Dominicana', 22, 'Regular'),
    ('Ana Martinez', 'ana.martinez@gmail.com', '2026-01-02', 'Republica Dominicana', 50, 'Nuevo'),
    ('Laura Gomez', 'laura.gomez@gmail.com', '2025-07-21', 'Republica Dominicana', 37, 'Premium'),
    ('Daniela Cabrera', 'daniela.cabrera@gmail.com', '2025-08-12', 'Mexico', 28, 'Regular'),
    ('Luis Fernandez', 'luis.fernandez@gmail.com', '2026-02-07', 'Colombia', 40, 'Nuevo'),
    ('Claudia Mateo', 'claudia.mateo@gmail.com', '2026-02-23', 'Estados Unidos', 26, 'Regular'),
    ('Pedro Almonte', 'pedro.almonte@gmail.com', '2025-07-12', 'Republica Dominicana', 47, 'Regular')
) AS v(Nombre, Correo, Fecha, Pais, Edad, Tipo)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Clientes c WHERE (v.Correo IS NOT NULL AND c.Correo = v.Correo) OR (v.Correo IS NULL AND c.Correo IS NULL AND c.Nombre = v.Nombre));
GO

INSERT INTO dbo.Productos (Nombre, Descripcion, Categoria, Precio)
SELECT v.Nombre, v.Descripcion, v.Categoria, v.Precio FROM (VALUES
    ('Auriculares Bluetooth X200', 'Auriculares inalambricos con cancelacion de ruido', 'Electronica', 2499.00),
    ('Licuadora Turbo 600W', 'Licuadora de vaso de vidrio de 1.5 litros', 'Hogar', 1850.00),
    ('Smartwatch Fit Pro', 'Reloj inteligente con monitor de ritmo cardiaco', 'Electronica', 3999.00),
    ('Cafe Santo Domingo 1lb', 'Cafe molido tostado medio', 'Alimentos', 385.00),
    ('Mochila Urbana 25L', 'Mochila resistente al agua con puerto USB', 'Accesorios', 1299.00),
    ('Ventilador de Pie 16in', 'Ventilador oscilante de tres velocidades', 'Hogar', 2150.00),
    ('Teclado Mecanico RGB', 'Teclado mecanico con retroiluminacion', 'Electronica', 2799.00),
    ('Botella Termica 750ml', 'Mantiene el frio por 24 horas', 'Accesorios', 650.00),
    ('Freidora de Aire 4L', 'Freidora de aire con panel digital', 'Hogar', 4500.00),
    ('Camiseta Algodon Basica', 'Camiseta unisex de algodon', 'Ropa', 450.00),
    ('Cargador Rapido 65W', 'Cargador USB-C de carga rapida', 'Electronica', 990.00),
    ('Lampara LED Escritorio', 'Lampara con tres niveles de brillo', 'Hogar', 875.00),
    ('Zapatillas Running Air', 'Zapatillas livianas para correr', 'Ropa', 3250.00),
    ('Bocina Portatil Boom', 'Bocina bluetooth resistente al agua', 'Electronica', 1750.00)
) AS v(Nombre, Descripcion, Categoria, Precio)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Productos p WHERE p.Nombre = v.Nombre);
GO

INSERT INTO dbo.Fuentes (Nombre, Tipo)
SELECT v.Nombre, v.Tipo FROM (VALUES
    ('Instagram', 'Red Social'),
    ('Facebook', 'Red Social'),
    ('X (Twitter)', 'Red Social'),
    ('Formulario Web', 'Web'),
    ('Google Reviews', 'Resena Publica'),
    ('WhatsApp', 'Mensajeria')
) AS v(Nombre, Tipo)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Fuentes f WHERE f.Nombre = v.Nombre);
GO

INSERT INTO dbo.Eventos (Nombre, Tipo, ProductoID, FechaInicio, FechaFin)
SELECT v.Nombre, v.Tipo, (SELECT TOP 1 ProductoID FROM dbo.Productos WHERE Nombre = v.Producto), CAST(v.Inicio AS DATE), CAST(v.Fin AS DATE) FROM (VALUES
    ('Lanzamiento Lampara LED', 'Lanzamiento', 'Lampara LED Escritorio', '2026-01-12', '2026-01-31'),
    ('Campana San Valentin', 'Campana', NULL, '2026-02-07', '2026-02-14'),
    ('Promocion Cargador Rapido', 'Promocion', 'Cargador Rapido 65W', '2026-03-16', '2026-03-31'),
    ('Lanzamiento Bocina Boom', 'Lanzamiento', 'Bocina Portatil Boom', '2026-07-06', '2026-07-20'),
    ('Campana Regreso a Clases', 'Campana', NULL, '2026-08-17', '2026-08-31'),
    ('Lanzamiento Zapatillas Air', 'Lanzamiento', 'Zapatillas Running Air', '2026-09-01', '2026-09-15')
) AS v(Nombre, Tipo, Producto, Inicio, Fin)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Eventos e WHERE e.Nombre = v.Nombre AND e.FechaInicio = CAST(v.Inicio AS DATE));
GO

SELECT 'Clientes' AS Tabla, COUNT(*) AS Filas FROM dbo.Clientes UNION ALL SELECT 'Productos', COUNT(*) FROM dbo.Productos
UNION ALL SELECT 'Fuentes', COUNT(*) FROM dbo.Fuentes UNION ALL SELECT 'Eventos', COUNT(*) FROM dbo.Eventos;
GO
