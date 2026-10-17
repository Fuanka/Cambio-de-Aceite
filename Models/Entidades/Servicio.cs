using Models.Enumeraciones;
using Models.Excepciones;

namespace Models.Entidades
{
    public abstract class Servicio
    {
        public string Codigo { get; }
        public string Nombre { get; }
        public string Descripcion { get; }
        public int MinutosEstimados { get; }
        public NivelMecanico NivelRequerido { get; }

        protected Servicio(string codigo, string nombre, string descripcion, int minutosEstimados, NivelMecanico nivelRequerido)
        {
            if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre) || minutosEstimados <= 0)
                throw new ReglaNegocioException("Datos de servicio inválidos.");
            Codigo = codigo;
            Nombre = nombre;
            Descripcion = descripcion;
            MinutosEstimados = minutosEstimados;
            NivelRequerido = nivelRequerido;
        }

        public abstract int DiasGarantia { get; }
        public abstract int KilometrosGarantia { get; }
        public abstract bool RequiereAutorizacion { get; }

        /// <summary>El correctivo no se puede cotizar sin saber qué falla tiene la moto.</summary>
        public abstract bool RequiereDiagnosticoPrevio { get; }

        /// <summary>El ajuste rápido se atiende de inmediato y no hace fila.</summary>
        public abstract bool EntraAFila { get; }

        public bool TieneGarantia => DiasGarantia > 0 || KilometrosGarantia > 0;
    }
}
