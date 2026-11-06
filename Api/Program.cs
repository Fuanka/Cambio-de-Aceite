using System.Text.Json.Serialization;
using Api.Middleware;
using Business;
using Data;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión vive en appsettings.json (sección ConnectionStrings), no dentro del código.
string cadenaConexion = builder.Configuration.GetConnectionString("TallerMultimarca")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'TallerMultimarca' en appsettings.json.");


// Composición: la API registra las dos capas de abajo, pero los controladores solo conocen interfaces de servicio.
builder.Services.AddData(cadenaConexion);
builder.Services.AddBusiness();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ManejadorDeExcepciones>();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();
