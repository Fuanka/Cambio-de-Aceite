using Business.DTOs.Motocicleta;

namespace Business.Interfaces
{
    public interface IMotocicletaServicio
    {
        IReadOnlyCollection<MotocicletaDto> Listar();
        MotocicletaDto Crear(CrearMotocicletaDto dto);
    }
}
