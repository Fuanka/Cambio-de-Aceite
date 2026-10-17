using Models.Enumeraciones;
using Models.Excepciones;

namespace Models.Entidades
{
    public class ClienteConvenio : Cliente
    {
        public const decimal DescuentoMinimo = 0.15m;
        public const decimal DescuentoMaximo = 0.25m;

        public decimal DescuentoNegociado { get; private set; }

        public ClienteConvenio(int id, string documento, string nombre, string telefono, string correo, decimal descuentoNegociado)
            : base(id, documento, nombre, telefono, correo)
        {
            if (descuentoNegociado < DescuentoMinimo || descuentoNegociado > DescuentoMaximo)
                throw new ReglaNegocioException($"El descuento de un convenio debe estar entre {DescuentoMinimo} y {DescuentoMaximo}.");
            DescuentoNegociado = descuentoNegociado;
        }

        public override TipoCliente Tipo => TipoCliente.Convenio;
        public override double FactorFila => 1.5;
        public override decimal ObtenerDescuento() => DescuentoNegociado;

        // La autorización la da la entidad con su número externo, no el usuario final.
        public override bool AutorizaSinConsulta(decimal valor) => true;

        public override void ValidarPuedeAbrirOrden(string? numeroAutorizacionExterna)
        {
            if (string.IsNullOrWhiteSpace(numeroAutorizacionExterna))
                throw new ReglaNegocioException("El cliente de convenio necesita un número de autorización externo antes de iniciar cualquier trabajo.");
        }
    }
}
