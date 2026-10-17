using Models.Enumeraciones;

namespace Models.Entidades
{
    public sealed class Preventivo
    (
        string codigo,
        string nombre,
        string descripcion,
        int minutosEstimados,
        NivelMecanico nivelRequerido,
        int kilometrajeObjetivo
    ) : Servicio(codigo, nombre, descripcion, minutosEstimados, nivelRequerido)
    {
        public int KilometrajeObjetivo { get; } = kilometrajeObjetivo;
        public override int DiasGarantia => 30;
        public override int KilometrosGarantia => 1000;
        public override bool RequiereAutorizacion => false;
        public override bool RequiereDiagnosticoPrevio => false;
        public override bool EntraAFila => true;
    }
}
