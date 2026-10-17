using System.Text.Json.Serialization;
using Api.Middleware;
using Business;
using Data;

var builder = WebApplication.CreateBuilder(args);

// Composición: la API registra las dos capas de abajo, pero los controladores solo conocen interfaces de servicio.
builder.Services.AddData();
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
