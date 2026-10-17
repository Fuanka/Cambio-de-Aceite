using Business.DTOs.Catalogo;

namespace Business.Interfaces
{
    public interface ICatalogoServicio
    {
        IReadOnlyCollection<ServicioDto> Servicios();
        IReadOnlyCollection<MecanicoDto> Mecanicos();
        IReadOnlyCollection<RepuestoDto> Repuestos();
    }
}
