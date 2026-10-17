using Models.Enumeraciones;
using Models.Excepciones;

namespace Models.Entidades
{
    public class ClienteFlota : Cliente
    {
        public decimal CupoCredito { get; private set; }
        public decimal MontoAutorizado { get; private set; }
        public decimal SaldoUtilizado { get; private set; }
        public bool TieneFacturasVencidas { get; private set; }

        public ClienteFlota(int id, string documento, string nombre, string telefono, string correo,
                            decimal cupoCredito, decimal montoAutorizado, bool tieneFacturasVencidas = false)
            : base(id, documento, nombre, telefono, correo)
        {
            if (cupoCredito < 0 || montoAutorizado < 0)
                throw new ReglaNegocioException("Los valores de crédito no pueden ser negativos.");
            CupoCredito = cupoCredito;
            MontoAutorizado = montoAutorizado;
            TieneFacturasVencidas = tieneFacturasVencidas;
        }

        public override TipoCliente Tipo => TipoCliente.Flota;
        public override double FactorFila => 1.0;
        public override decimal ObtenerDescuento() => 0.12m;

        // Hasta el monto pactado autoriza sin consulta previa.
        public override bool AutorizaSinConsulta(decimal valor) => valor <= MontoAutorizado;

        // Bloqueada solo si superó el cupo Y tiene facturas vencidas (así lo dice el documento).
        public override void ValidarPuedeAbrirOrden(string? numeroAutorizacionExterna)
        {
            if (SaldoUtilizado > CupoCredito && TieneFacturasVencidas)
                throw new ReglaNegocioException("El cliente de flota superó su cupo de crédito y tiene facturas vencidas.");
        }

        public override void RegistrarCargo(decimal valor)
        {
            if (valor < 0)
                throw new ReglaNegocioException("El valor del cargo no puede ser negativo.");
            SaldoUtilizado += valor;
        }
    }
}
