using Models.Enumeraciones;
using Models.Excepciones;

namespace Models.Entidades
{
    /// <summary>
    /// Cliente del taller. Es abstracta: lo que cambia entre Particular, Flota y Convenio
    /// (descuento, fila, autorización, bloqueos) lo decide cada subclase, sin switch por tipo.
    /// </summary>
    public abstract class Cliente
    {
        public int Id { get; private set; }
        public string Documento { get; private set; }
        public string Nombre { get; private set; }
        public string Telefono { get; private set; }
        public string Correo { get; private set; }

        public abstract TipoCliente Tipo { get; }

        /// <summary>Multiplica el tiempo de ejecución según su lugar en la fila (flota 1.0, convenio 1.5, particular 2.0).</summary>
        public abstract double FactorFila { get; }

        protected Cliente(int id, string documento, string nombre, string telefono, string correo)
        {
            if (id < 0)
                throw new ReglaNegocioException("El id no puede ser negativo.");
            if (string.IsNullOrWhiteSpace(documento) || string.IsNullOrWhiteSpace(nombre))
                throw new ReglaNegocioException("Documento y nombre son obligatorios.");

            Id = id;
            Documento = documento.Trim();
            Nombre = nombre.Trim();
            Telefono = telefono ?? string.Empty;
            Correo = correo ?? string.Empty;
        }

        /// <summary>Descuento como fracción (0.12 = 12 %).</summary>
        public abstract decimal ObtenerDescuento();

        /// <summary>¿El valor cotizado se puede ejecutar sin pedirle autorización al cliente?</summary>
        public abstract bool AutorizaSinConsulta(decimal valor);

        /// <summary>Lanza si algo impide abrir una orden (convenio sin número externo, flota sin cupo...).</summary>
        public virtual void ValidarPuedeAbrirOrden(string? numeroAutorizacionExterna) { }

        /// <summary>Se llama al entregar la orden. Solo la flota lo usa (cargo a crédito).</summary>
        public virtual void RegistrarCargo(decimal valor) { }
    }
}
