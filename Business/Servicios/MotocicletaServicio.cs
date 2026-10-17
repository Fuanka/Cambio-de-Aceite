using Business.DTOs.Motocicleta;
using Business.Interfaces;
using Business.Mapeos;
using Models.Entidades;
using Models.Excepciones;

namespace Business.Servicios
{
    public class MotocicletaServicio(IRepositorioMotocicletas motos, IRepositorioClientes clientes) : IMotocicletaServicio
    {
        public IReadOnlyCollection<MotocicletaDto> Listar()
            => motos.ObtenerTodos().Select(m => Mapeador.ADto(m)).ToList();

        public MotocicletaDto Crear(CrearMotocicletaDto dto)
        {
            if (clientes.ObtenerPorId(dto.ClienteId) is null)
                throw new NoEncontradoException("El cliente no existe.");
            if (motos.BuscarPorPlaca(dto.Placa) is not null)
                throw new ConflictoException("Ya existe una motocicleta con esa placa.");

            var moto = new Motocicleta(
                motos.SiguienteId(),
                dto.Placa,
                dto.Marca,
                dto.Modelo,
                dto.Cilindraje,
                dto.Kilometraje,
                dto.ClienteId);
            motos.Agregar(moto);
            return Mapeador.ADto(moto);
        }
    }
}
