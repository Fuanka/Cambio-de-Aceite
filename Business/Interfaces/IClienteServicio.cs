using Business.DTOs.Cliente;

namespace Business.Interfaces
{
    public interface IClienteServicio
    {
        IReadOnlyCollection<ClienteDto> Listar();
        ClienteDto Obtener(int id);
        ClienteDto Crear(CrearClienteDto dto);
    }
}
