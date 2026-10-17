using Business.DTOs.Catalogo;
using Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>Servicios, mecánicos y repuestos disponibles (para saber qué códigos usar al crear una orden).</summary>
    [ApiController]
    [Route("api/catalogo")]
    public class CatalogoController(ICatalogoServicio servicio) : ControllerBase
    {
        [HttpGet("servicios")]
        public ActionResult<IReadOnlyCollection<ServicioDto>> Servicios() => Ok(servicio.Servicios());

        [HttpGet("mecanicos")]
        public ActionResult<IReadOnlyCollection<MecanicoDto>> Mecanicos() => Ok(servicio.Mecanicos());

        [HttpGet("repuestos")]
        public ActionResult<IReadOnlyCollection<RepuestoDto>> Repuestos() => Ok(servicio.Repuestos());
    }
}
