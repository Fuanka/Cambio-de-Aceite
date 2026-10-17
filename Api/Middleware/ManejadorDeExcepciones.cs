using Microsoft.AspNetCore.Mvc;
using Models.Excepciones;

namespace Api.Middleware
{
    /// <summary>
    /// Traduce las excepciones de negocio a respuestas HTTP claras.
    /// Así los controladores no necesitan try/catch ni conocer las reglas.
    /// </summary>
    public class ManejadorDeExcepciones(RequestDelegate siguiente, ILogger<ManejadorDeExcepciones> logger)
    {
        public async Task InvokeAsync(HttpContext contexto)
        {
            try
            {
                await siguiente(contexto);
            }
            catch (ReglaNegocioException ex)
            {
                int codigo = ex switch
                {
                    NoEncontradoException => StatusCodes.Status404NotFound,
                    ConflictoException => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status422UnprocessableEntity
                };
                logger.LogWarning("Regla de negocio ({Tipo}): {Mensaje}", ex.GetType().Name, ex.Message);
                await Responder(contexto, codigo, ex.GetType().Name, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inesperado");
                await Responder(contexto, StatusCodes.Status500InternalServerError, "ErrorInterno",
                    "Ocurrió un error inesperado. Intente de nuevo.");
            }
        }

        private static async Task Responder(HttpContext contexto, int codigo, string titulo, string detalle)
        {
            if (contexto.Response.HasStarted) return;

            contexto.Response.StatusCode = codigo;
            await contexto.Response.WriteAsJsonAsync(new ProblemDetails { Status = codigo, Title = titulo, Detail = detalle });
        }
    }
}
