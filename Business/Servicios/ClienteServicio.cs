using Business.DTOs.Cliente;
using Business.Fabricas;
using Business.Interfaces;
using Business.Mapeos;
using Models.Excepciones;

namespace Business.Servicios
{
    public class ClienteServicio(IRepositorioClientes repo) : IClienteServicio
    {
        public IReadOnlyCollection<ClienteDto> Listar()
            => repo.ObtenerTodos().Select(c => Mapeador.ADto(c)).ToList();

        public ClienteDto Obtener(int id)
        {
            var cliente = repo.ObtenerPorId(id)
                ?? throw new NoEncontradoException($"No existe el cliente {id}.");
            return Mapeador.ADto(cliente);
        }

        public ClienteDto Crear(CrearClienteDto dto)
        {
            if (repo.BuscarPorDocumento(dto.Documento) is not null)
                throw new ConflictoException("Ya existe un cliente con ese documento.");

            var cliente = FabricaClientes.Crear(repo.SiguienteId(), dto);
            repo.Agregar(cliente);
            return Mapeador.ADto(cliente);
        }
    }
}
