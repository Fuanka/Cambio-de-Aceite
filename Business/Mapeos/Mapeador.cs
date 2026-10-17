using Business.DTOs.Catalogo;
using Business.DTOs.Cliente;
using Business.DTOs.Motocicleta;
using Business.DTOs.Orden;
using Models.Entidades;

namespace Business.Mapeos
{
    /// <summary>Convierte entidades en DTOs: las entidades nunca salen de la capa de negocio.</summary>
    public static class Mapeador
    {
        public static ClienteDto ADto(Cliente c)
        {
            var flota = c as ClienteFlota;
            return new ClienteDto(c.Id, c.Documento, c.Nombre, c.Telefono, c.Correo, c.Tipo, c.ObtenerDescuento(),
                flota?.CupoCredito, flota?.MontoAutorizado, flota?.SaldoUtilizado, flota?.TieneFacturasVencidas);
        }

        public static MotocicletaDto ADto(Motocicleta m)
            => new(m.Id, m.Placa, m.Marca, m.Modelo, m.Cilindraje, m.Kilometraje, m.ClienteId);

        public static ServicioDto ADto(Servicio s)
            => new(s.Codigo, s.Nombre, s.GetType().Name, s.Descripcion, s.MinutosEstimados, s.NivelRequerido,
                   s.DiasGarantia, s.KilometrosGarantia, s.RequiereAutorizacion);

        public static MecanicoDto ADto(Mecanico m)
            => new(m.Id, m.Nombre, m.Nivel, m.TarifaPorHora, m.Disponible);

        public static RepuestoDto ADto(Repuesto r)
            => new(r.Codigo, r.Nombre, r.PrecioVenta, r.CantidadStock, r.StockMinimo, r.Origen, r.DiasEspera, r.BajoMinimo);

        public static LineaDto ADto(LineaServicio l)
            => new(l.Id, l.Servicio.Codigo, l.Servicio.Nombre, l.Mecanico.Id, l.Mecanico.Nombre, l.Diagnostico, l.Autorizada,
                   l.CalcularCostoManoDeObra(),
                   l.Repuestos.Select(u => new UsoRepuestoDto(u.Repuesto.Codigo, u.Repuesto.Nombre, u.Cantidad,
                                                              u.PrecioVentaUnitario, u.Subtotal, u.Descontado)).ToList(),
                   l.Garantias.Select(g => new GarantiaDto(g.Id, g.LineaServicioId, g.FechaInicio, g.FechaVencimiento,
                                                           g.KilometrajeLimite, g.Anulada)).ToList());

        public static OrdenDto ADto(OrdenDeTrabajo o)
            => new(o.Id, o.ClienteId, o.MotocicletaId, o.KilometrajeIngreso, o.FechaIngreso, o.FechaCotizacion,
                   o.FechaEntregaPrometida, o.FechaEntregaReal, o.Estado, o.NumeroAutorizacionExterna, o.ValorTotal,
                   o.Lineas.Select(l => ADto(l)).ToList());
    }
}
