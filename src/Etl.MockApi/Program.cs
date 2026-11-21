using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var clave = app.Configuration["ApiKey"];
var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "comentarios_api.json"));
var datos = JsonSerializer.Deserialize<List<Comentario>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new();

app.MapGet("/", () => "Mock API de comentarios activa.");

app.MapGet("/api/comentarios", (HttpContext ctx, int? pagina, int? tamano) =>
{
    if (string.IsNullOrEmpty(clave) || ctx.Request.Headers["X-Api-Key"] != clave)
        return Results.Unauthorized();

    var p = Math.Max(pagina ?? 1, 1);
    var t = Math.Clamp(tamano ?? 50, 1, 200);
    var items = datos.Skip((p - 1) * t).Take(t).ToList();

    return Results.Ok(new { pagina = p, tamano = t, total = datos.Count, hayMas = p * t < datos.Count, datos = items });
});

app.Run();

record Comentario(string Id, string? Usuario, string? Correo, string? Producto, string? Plataforma, int? Calificacion, string? Texto, string? Fecha);
