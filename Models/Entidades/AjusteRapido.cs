using Models.Enumeraciones;
using Models.Excepciones;

namespace Models.Entidades
{
    public sealed class AjusteRapido : Servicio
    {
        public AjusteRapido(string codigo, string nombre, string descripcion, int minutosEstimados, NivelMecanico nivelRequerido)
            : base(codigo, nombre, descripcion, minutosEstimados, nivelRequerido)
        {
            if (minutosEstimados >= 30)
                throw new ReglaNegocioException("Un ajuste rápido dura menos de 30 minutos.");
        }

        public override int DiasGarantia => 0;
        public override int KilometrosGarantia => 0;
        public override bool RequiereAutorizacion => false;
        public override bool RequiereDiagnosticoPrevio => false;
        public override bool EntraAFila => false;
    }
}
