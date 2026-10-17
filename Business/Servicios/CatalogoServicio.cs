using Business.DTOs.Catalogo;
using Business.Interfaces;
using Business.Mapeos;

namespace Business.Servicios
{
    public class CatalogoServicio(IRepositorioCatalogo catalogo) : ICatalogoServicio
    {
        public IReadOnlyCollection<ServicioDto> Servicios()
            => catalogo.ObtenerServicios().Select(s => Mapeador.ADto(s)).ToList();

        public IReadOnlyCollection<MecanicoDto> Mecanicos()
            => catalogo.ObtenerMecanicos().Select(m => Mapeador.ADto(m)).ToList();

        public IReadOnlyCollection<RepuestoDto> Repuestos()
            => catalogo.ObtenerRepuestos().Select(r => Mapeador.ADto(r)).ToList();
    }
}
