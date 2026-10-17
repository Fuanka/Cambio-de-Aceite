using Business.DTOs.Cliente;
using Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/clientes")]
    public class ClientesController(IClienteServicio servicio) : ControllerBase
    {
        [HttpGet]
        public ActionResult<IReadOnlyCollection<ClienteDto>> Listar() => Ok(servicio.Listar());

        [HttpGet("{id:int}")]
        public ActionResult<ClienteDto> Obtener(int id) => Ok(servicio.Obtener(id));

        [HttpPost]
        [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
        public ActionResult<ClienteDto> Crear([FromBody] CrearClienteDto dto)
        {
            var creado = servicio.Crear(dto);
            return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
        }
    }
}
