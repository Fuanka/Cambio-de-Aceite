using Models.Excepciones;

namespace Models.Entidades
{
    /// <summary>
    /// Repuesto asignado a un servicio de la orden. Guarda el precio de venta del momento
    /// para que un cambio de precio posterior no altere la cotización.
    /// </summary>
    public class UsoRepuesto
    {
        public Repuesto Repuesto { get; }
        public int Cantidad { get; }
        public decimal PrecioVentaUnitario { get; }
        public bool Descontado { get; private set; }

        public decimal Subtotal => PrecioVentaUnitario * Cantidad;

        /// <summary>Todavía no se consumió y en bodega no alcanzan las unidades: hay que esperar el pedido.</summary>
        public bool EstaPendienteDeStock => !Descontado && !Repuesto.HayExistencias(Cantidad);

        public UsoRepuesto(Repuesto repuesto, int cantidad)
        {
            if (repuesto is null)
                throw new ReglaNegocioException("El repuesto es obligatorio.");
            if (cantidad <= 0)
                throw new ReglaNegocioException("La cantidad de repuestos debe ser mayor que cero.");

            Repuesto = repuesto;
            Cantidad = cantidad;
            PrecioVentaUnitario = repuesto.PrecioVenta;
        }

        // Solo para reconstruir desde la base de datos: conserva el precio y el estado que se guardaron.
        private UsoRepuesto(Repuesto repuesto, int cantidad, decimal precioVentaUnitario, bool descontado)
        {
            Repuesto = repuesto;
            Cantidad = cantidad;
            PrecioVentaUnitario = precioVentaUnitario;
            Descontado = descontado;
        }

        public static UsoRepuesto Restaurar(Repuesto repuesto, int cantidad, decimal precioVentaUnitario, bool descontado)
            => new(repuesto, cantidad, precioVentaUnitario, descontado);

        public void Consumir()
        {
            if (Descontado) return;
            Repuesto.Descontar(Cantidad);
            Descontado = true;
        }
    }
}
