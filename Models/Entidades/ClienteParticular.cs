using Models.Enumeraciones;

namespace Models.Entidades
{
    public class ClienteParticular(int id, string documento, string nombre, string telefono, string correo)
        : Cliente(id, documento, nombre, telefono, correo)
    {
        public override TipoCliente Tipo => TipoCliente.Particular;
        public override double FactorFila => 2.0;
        public override decimal ObtenerDescuento() => 0m;

        // El particular siempre autoriza él mismo lo que se le cotiza.
        public override bool AutorizaSinConsulta(decimal valor) => false;
    }
}
