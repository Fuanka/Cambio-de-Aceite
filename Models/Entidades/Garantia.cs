using Models.Excepciones;

namespace Models.Entidades
{
    public record ResultadoGarantia(bool Vigente, string Motivo);

    public class Garantia
    {
        public int Id { get; }
        public int OrdenOriginalId { get; }
        public int LineaServicioId { get; }
        public DateTime FechaInicio { get; }
        public DateTime FechaVencimiento { get; }
        public int KilometrajeInicio { get; }
        public int KilometrajeLimite { get; }
        public string Condiciones { get; }
        public bool Anulada { get; private set; }
        public string? MotivoAnulacion { get; private set; }

        // La garantía corre desde la ENTREGA: el kilometraje base es el que tenía la moto al retirarla.
        public Garantia(int id, int ordenOriginalId, int lineaServicioId, DateTime fechaInicio, int dias, int kilometrajeEntrega, int kilometrosGarantia, string condiciones)
        {
            Id = id;
            OrdenOriginalId = ordenOriginalId;
            LineaServicioId = lineaServicioId;
            FechaInicio = fechaInicio;
            FechaVencimiento = fechaInicio.AddDays(dias);
            KilometrajeInicio = kilometrajeEntrega;
            KilometrajeLimite = kilometrajeEntrega + kilometrosGarantia;
            Condiciones = condiciones;
        }

        public void Anular(string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ReglaNegocioException("Indique el motivo de la anulación.");
            Anulada = true;
            MotivoAnulacion = motivo;
        }

        /// <param name="kilometrajeActual">Kilometraje que reporta el cliente en el reclamo.</param>
        /// <param name="kilometrajeRegistrado">Último kilometraje que el taller tiene registrado para la moto.</param>
        public ResultadoGarantia Validar(DateTime fechaReclamo, int kilometrajeActual, int kilometrajeRegistrado, bool intervenidaPorOtroTaller)
        {
            if (Anulada)
                return new ResultadoGarantia(false, $"La garantía fue anulada: {MotivoAnulacion}");
            if (intervenidaPorOtroTaller)
                return new ResultadoGarantia(false, "Se pierde la garantía: la moto fue intervenida por otro taller.");
            if (kilometrajeActual < kilometrajeRegistrado)
                return new ResultadoGarantia(false, "Se pierde la garantía: el kilometraje reportado es inconsistente con el registrado.");
            if (fechaReclamo.Date > FechaVencimiento.Date)
                return new ResultadoGarantia(false, "La garantía venció por tiempo.");
            if (kilometrajeActual > KilometrajeLimite)
                return new ResultadoGarantia(false, "La garantía venció por kilometraje.");
            return new ResultadoGarantia(true, "Garantía vigente por tiempo y por kilometraje.");
        }
    }
}
