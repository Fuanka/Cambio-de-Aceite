using Business.DTOs.Motocicleta;
using Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/motocicletas")]
    public class MotocicletasController(IMotocicletaServicio servicio) : ControllerBase
    {
        [HttpGet]
        public ActionResult<IReadOnlyCollection<MotocicletaDto>> Listar() => Ok(servicio.Listar());

        [HttpPost]
        [ProducesResponseType(typeof(MotocicletaDto), StatusCodes.Status201Created)]
        public ActionResult<MotocicletaDto> Crear([FromBody] CrearMotocicletaDto dto)
        {
            var creada = servicio.Crear(dto);
            return Created($"api/motocicletas/{creada.Id}", creada);
        }
    }
}
