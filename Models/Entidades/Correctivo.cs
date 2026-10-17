using Models.Enumeraciones;

namespace Models.Entidades
{
    // El diagnóstico ya no vive aquí: es de cada caso, y por eso está en LineaServicio.Diagnostico.
    public sealed class Correctivo(
        string codigo,
        string nombre,
        string descripcion,
        int minutosEstimados,
        NivelMecanico nivelRequerido)
     : Servicio(codigo, nombre, descripcion, minutosEstimados, nivelRequerido)
    {
        public override int DiasGarantia => 90;
        public override int KilometrosGarantia => 3000;
        public override bool RequiereAutorizacion => true;
        public override bool RequiereDiagnosticoPrevio => true;
        public override bool EntraAFila => true;
    }
}
