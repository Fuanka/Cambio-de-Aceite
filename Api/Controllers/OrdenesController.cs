using Business.DTOs.Orden;
using Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>Controlador delgado: recibe, delega en la capa de negocio y responde. No tiene reglas.</summary>
    [ApiController]
    [Route("api/ordenes")]
    public class OrdenesController(IOrdenServicio servicio) : ControllerBase
    {
        [HttpGet]
        public ActionResult<IReadOnlyCollection<OrdenDto>> Listar() => Ok(servicio.Listar());

        [HttpGet("{id:int}")]
        public ActionResult<OrdenDto> Obtener(int id) => Ok(servicio.Obtener(id));

        [HttpPost]
        [ProducesResponseType(typeof(OrdenDto), StatusCodes.Status201Created)]
        public ActionResult<OrdenDto> Crear([FromBody] CrearOrdenDto dto)
        {
            var creada = servicio.Crear(dto);
            return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
        }

        [HttpPost("{id:int}/autorizar")]
        public ActionResult<OrdenDto> Autorizar(int id) => Ok(servicio.Autorizar(id));

        [HttpPost("{id:int}/iniciar-ejecucion")]
        public ActionResult<OrdenDto> IniciarEjecucion(int id) => Ok(servicio.IniciarEjecucion(id));

        [HttpPost("{id:int}/reanudar")]
        public ActionResult<OrdenDto> Reanudar(int id) => Ok(servicio.Reanudar(id));

        [HttpPost("{id:int}/terminar")]
        public ActionResult<OrdenDto> Terminar(int id) => Ok(servicio.Terminar(id));

        [HttpPost("{id:int}/entregar")]
        public ActionResult<OrdenDto> Entregar(int id, [FromBody] EntregarOrdenDto? dto = null)
            => Ok(servicio.Entregar(id, dto?.KilometrajeEntrega));

        [HttpPost("{id:int}/cerrar-sin-ejecutar")]
        public ActionResult<OrdenDto> CerrarSinEjecutar(int id, [FromBody] CerrarOrdenDto dto)
            => Ok(servicio.CerrarSinEjecutar(id, dto.Motivo));

        [HttpGet("{id:int}/garantias")]
        public ActionResult<ConsultaGarantiaDto> ConsultarGarantias(
            int id, [FromQuery] int kilometrajeActual, [FromQuery] DateTime? fechaReclamo, [FromQuery] bool intervenidaPorOtroTaller = false)
            => Ok(servicio.ConsultarGarantias(id, kilometrajeActual, fechaReclamo, intervenidaPorOtroTaller));
    }
}
