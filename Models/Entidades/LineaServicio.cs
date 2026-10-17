using Models.Excepciones;

namespace Models.Entidades
{
    /// <summary>
    /// Un servicio dentro de la orden, con su mecánico, diagnóstico, autorización, repuestos y garantía.
    /// </summary>
    public class LineaServicio
    {
        private readonly List<UsoRepuesto> _repuestos = new();
        private readonly List<Garantia> _garantias = new();

        public int Id { get; }
        public Servicio Servicio { get; }
        public Mecanico Mecanico { get; private set; }
        public string Diagnostico { get; private set; }
        public bool Autorizada { get; private set; }
        public DateTime? FechaAutorizacion { get; private set; }
        public DateTime? FechaEjecucion { get; private set; }
        public IReadOnlyList<UsoRepuesto> Repuestos => _repuestos;
        public IReadOnlyList<Garantia> Garantias => _garantias;

        public LineaServicio(int id, Servicio servicio, Mecanico mecanico, string diagnostico = "")
        {
            if (servicio is null)
                throw new ReglaNegocioException("El servicio es obligatorio.");
            if (mecanico is null)
                throw new ReglaNegocioException("La línea necesita un mecánico asignado.");
            // La regla vive en el dominio: ninguna capa puede asignar un mecánico que no pueda hacer el trabajo.
            if (!mecanico.PuedeEjecutar(servicio))
                throw new ReglaNegocioException(
                    $"El mecánico {mecanico.Nombre} no está disponible o no tiene el nivel requerido ({servicio.NivelRequerido}) para '{servicio.Nombre}'.");

            Id = id;
            Servicio = servicio;
            Mecanico = mecanico;
            Diagnostico = diagnostico ?? string.Empty;
            Autorizada = !servicio.RequiereAutorizacion;
        }

        public void RegistrarDiagnostico(string diagnostico)
        {
            if (string.IsNullOrWhiteSpace(diagnostico))
                throw new ReglaNegocioException("El diagnóstico no puede estar vacío.");
            Diagnostico = diagnostico;
        }

        public void Autorizar(DateTime fecha)
        {
            Autorizada = true;
            FechaAutorizacion = fecha;
        }

        public UsoRepuesto AgregarRepuesto(Repuesto repuesto, int cantidad)
        {
            var uso = new UsoRepuesto(repuesto, cantidad);
            _repuestos.Add(uso);
            return uso;
        }

        public void MarcarEjecutada(DateTime fecha)
        {
            if (!Autorizada)
                throw new ReglaNegocioException("No se puede ejecutar un servicio sin autorización.");
            FechaEjecucion ??= fecha;
        }

        /// <summary>Minutos estimados por la tarifa por hora del mecánico que lo ejecuta.</summary>
        public decimal CalcularCostoManoDeObra() => Servicio.MinutosEstimados / 60m * Mecanico.TarifaPorHora;

        public decimal CalcularCostoRepuestos() => _repuestos.Sum(r => r.Subtotal);

        /// <summary>Crea la garantía del servicio (si su familia la tiene). Se llama al entregar la moto.</summary>
        public void GenerarGarantias(int ordenId, DateTime fechaEntrega, int kilometrajeEntrega)
        {
            _garantias.Clear();
            if (!Servicio.TieneGarantia) return;

            _garantias.Add(new Garantia(_garantias.Count + 1, ordenId, Id, fechaEntrega,
                Servicio.DiasGarantia, kilometrajeEntrega, Servicio.KilometrosGarantia,
                "Vence por tiempo o por kilometraje, lo que ocurra primero"));
        }
    }
}
