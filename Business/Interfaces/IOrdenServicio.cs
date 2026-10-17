using Business.DTOs.Orden;

namespace Business.Interfaces
{
    public interface IOrdenServicio
    {
        IReadOnlyCollection<OrdenDto> Listar();
        OrdenDto Obtener(int id);
        OrdenDto Crear(CrearOrdenDto dto);
        OrdenDto Autorizar(int id);
        OrdenDto IniciarEjecucion(int id);
        OrdenDto Reanudar(int id);
        OrdenDto Terminar(int id);
        OrdenDto Entregar(int id, int? kilometrajeEntrega);
        OrdenDto CerrarSinEjecutar(int id, string motivo);
        ConsultaGarantiaDto ConsultarGarantias(int id, int kilometrajeActual, DateTime? fechaReclamo, bool intervenidaPorOtroTaller);
    }
}
