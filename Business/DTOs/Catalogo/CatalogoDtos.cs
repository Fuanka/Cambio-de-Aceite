using Models.Enumeraciones;

namespace Business.DTOs.Catalogo
{
    public record ServicioDto(string Codigo, string Nombre, string Tipo, string Descripcion, int MinutosEstimados,
                              NivelMecanico NivelRequerido, int DiasGarantia, int KilometrosGarantia, bool RequiereAutorizacion);

    public record MecanicoDto(int Id, string Nombre, NivelMecanico Nivel, decimal TarifaPorHora, bool Disponible);

    public record RepuestoDto(string Codigo, string Nombre, decimal PrecioVenta, int CantidadStock, int StockMinimo,
                              OrigenRepuesto Origen, int DiasEspera, bool BajoMinimo);
}
