using Models.Enumeraciones;
using Models.Excepciones;
using Models.Interfaces;

namespace Models.Entidades
{
    public class OrdenDeTrabajo
    {
        private const decimal DescuentoTresOMasServicios = 0.05m; // solo sobre la mano de obra
        private const int ServiciosParaDescuento = 3;
        public const int DiasHabilesLimiteAutorizacion = 5;

        // Máquina de estados: qué cambios son legítimos (incluye los retrocesos a "Cotizada").
        private static readonly Dictionary<EstadoOrden, EstadoOrden[]> Transiciones = new()
        {
            [EstadoOrden.Recibida] = new[] { EstadoOrden.EnDiagnostico, EstadoOrden.Cotizada, EstadoOrden.CerradaSinEjecutar },
            [EstadoOrden.EnDiagnostico] = new[] { EstadoOrden.Cotizada, EstadoOrden.CerradaSinEjecutar },
            [EstadoOrden.Cotizada] = new[] { EstadoOrden.Autorizada, EstadoOrden.CerradaSinEjecutar },
            [EstadoOrden.Autorizada] = new[] { EstadoOrden.EnEjecucion, EstadoOrden.Cotizada, EstadoOrden.CerradaSinEjecutar },
            [EstadoOrden.EnEjecucion] = new[] { EstadoOrden.EnEsperaDeRepuesto, EstadoOrden.Terminada, EstadoOrden.Cotizada },
            [EstadoOrden.EnEsperaDeRepuesto] = new[] { EstadoOrden.EnEjecucion, EstadoOrden.Cotizada },
            [EstadoOrden.Terminada] = new[] { EstadoOrden.Entregada },
            [EstadoOrden.Entregada] = Array.Empty<EstadoOrden>(),
            [EstadoOrden.CerradaSinEjecutar] = Array.Empty<EstadoOrden>()
        };

        // Privada: nadie puede agregar líneas saltándose las reglas de AgregarLinea.
        private readonly List<LineaServicio> _lineas = new();
        private bool _cotizacionDesactualizada;

        public int Id { get; }
        public int ClienteId { get; }
        public int MotocicletaId { get; }
        public int KilometrajeIngreso { get; }
        public DateTime FechaIngreso { get; }
        public string? NumeroAutorizacionExterna { get; }
        public DateTime? FechaCotizacion { get; private set; }
        public DateTime? FechaEntregaPrometida { get; private set; }
        public DateTime? FechaEntregaReal { get; private set; }
        public EstadoOrden Estado { get; private set; } = EstadoOrden.Recibida;
        public decimal ValorTotal { get; private set; }
        public IReadOnlyList<LineaServicio> Lineas => _lineas;
        public bool EstaAbierta => Estado is not (EstadoOrden.Entregada or EstadoOrden.CerradaSinEjecutar);

        public OrdenDeTrabajo(int id, int clienteId, int motocicletaId, int kilometrajeIngreso, DateTime fechaIngreso, string? numeroAutorizacionExterna = null)
        {
            if (id <= 0 || clienteId <= 0 || motocicletaId <= 0 || kilometrajeIngreso <= 0)
                throw new ReglaNegocioException("Datos de orden invalidos.");

            Id = id;
            ClienteId = clienteId;
            MotocicletaId = motocicletaId;
            KilometrajeIngreso = kilometrajeIngreso;
            FechaIngreso = fechaIngreso;
            NumeroAutorizacionExterna = string.IsNullOrWhiteSpace(numeroAutorizacionExterna) ? null : numeroAutorizacionExterna.Trim();
        }

        // ---------------------------------------------------------------- armado

        public void AgregarLinea(LineaServicio linea)
        {
            if (linea is null)
                throw new ReglaNegocioException("La línea es obligatoria.");
            if (!EstaAbierta || Estado == EstadoOrden.Terminada)
                throw new ConflictoException($"La orden está en estado {Estado} y ya no admite cambios.");

            _lineas.Add(linea);

            // Falla adicional: la orden retrocede a cotizada, se recotiza y espera autorización.
            if (Estado is EstadoOrden.Autorizada or EstadoOrden.EnEjecucion or EstadoOrden.EnEsperaDeRepuesto)
                CambiarEstado(EstadoOrden.Cotizada);

            if (Estado == EstadoOrden.Cotizada)
                _cotizacionDesactualizada = true;
        }

        public void IniciarDiagnostico()
        {
            CambiarEstado(EstadoOrden.EnDiagnostico);
        }

        // ---------------------------------------------------------------- flujo de estados

        public void Cotizar(Cliente cliente, ICalendarioLaboral calendario, DateTime ahora)
        {
            if (Estado is not (EstadoOrden.Recibida or EstadoOrden.EnDiagnostico or EstadoOrden.Cotizada))
                throw new ConflictoException($"Una orden en estado {Estado} no puede cotizarse.");
            if (_lineas.Count == 0)
                throw new ReglaNegocioException("La orden necesita al menos un servicio para cotizarse.");

            var sinDiagnostico = _lineas
                .Where(l => l.Servicio.RequiereDiagnosticoPrevio && string.IsNullOrWhiteSpace(l.Diagnostico))
                .Select(l => l.Servicio.Nombre)
                .ToList();
            if (sinDiagnostico.Count > 0)
                throw new ReglaNegocioException($"Falta el diagnóstico previo de: {string.Join(", ", sinDiagnostico)}.");

            CalcularValor(cliente);
            CalcularFechaPrometida(cliente, calendario);
            FechaCotizacion = ahora;
            _cotizacionDesactualizada = false;

            if (Estado != EstadoOrden.Cotizada)
                CambiarEstado(EstadoOrden.Cotizada);

            // Preventivos y ajustes no piden autorización; flota y convenio la tienen dada hasta cierto punto.
            if (_lineas.All(l => l.Autorizada) || cliente.AutorizaSinConsulta(ValorTotal))
                Autorizar(calendario, ahora, automatica: true);
        }

        public void Autorizar(ICalendarioLaboral calendario, DateTime ahora, bool automatica = false)
        {
            if (Estado != EstadoOrden.Cotizada)
                throw new ConflictoException($"Una orden en estado {Estado} no puede autorizarse.");

            if (!automatica)
            {
                if (CotizacionVencida(calendario, ahora))
                    throw new ReglaNegocioException(
                        $"La cotización lleva {DiasHabilesLimiteAutorizacion} o más días hábiles sin autorizarse: la orden debe cerrarse sin ejecutar.");
                if (_cotizacionDesactualizada)
                    throw new ReglaNegocioException("La cotización está desactualizada (se agregaron servicios): vuelva a cotizar antes de autorizar.");
            }

            foreach (var linea in _lineas.Where(l => !l.Autorizada))
                linea.Autorizar(ahora);

            CambiarEstado(EstadoOrden.Autorizada);
        }

        public void IniciarEjecucion(DateTime ahora)
        {
            CambiarEstado(EstadoOrden.EnEjecucion);
            foreach (var linea in _lineas)
                linea.MarcarEjecutada(ahora);
        }

        public void MarcarEnEsperaDeRepuesto() => CambiarEstado(EstadoOrden.EnEsperaDeRepuesto);

        public void Reanudar() => CambiarEstado(EstadoOrden.EnEjecucion);

        public void Terminar()
        {
            if (_lineas.SelectMany(l => l.Repuestos).Any(r => !r.Descontado))
                throw new ReglaNegocioException("No se puede terminar: hay repuestos sin consumir.");
            CambiarEstado(EstadoOrden.Terminada);
        }

        public void RegistrarEntrega(DateTime fecha, int? kilometrajeEntrega = null)
        {
            int km = kilometrajeEntrega ?? KilometrajeIngreso;
            if (km < KilometrajeIngreso)
                throw new ReglaNegocioException("El kilometraje de entrega no puede ser menor al de ingreso.");

            CambiarEstado(EstadoOrden.Entregada);
            FechaEntregaReal = fecha;

            // La garantía corre desde la entrega real, una por servicio.
            foreach (var linea in _lineas)
                linea.GenerarGarantias(Id, fecha, km);
        }

        public void CerrarSinEjecutar(string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ReglaNegocioException("Indique el motivo del cierre.");
            CambiarEstado(EstadoOrden.CerradaSinEjecutar);
        }

        // ---------------------------------------------------------------- reglas de cálculo

        /// <summary>
        /// Mano de obra (minutos x tarifa del mecánico) con 5 % menos si hay 3 o más servicios, más repuestos
        /// a precio de venta, y sobre el subtotal el descuento del tipo de cliente.
        /// </summary>
        public decimal CalcularValor(Cliente cliente)
        {
            decimal manoDeObra = _lineas.Sum(l => l.CalcularCostoManoDeObra());
            if (_lineas.Count >= ServiciosParaDescuento)
                manoDeObra *= 1 - DescuentoTresOMasServicios;

            decimal repuestos = _lineas.Sum(l => l.CalcularCostoRepuestos());
            decimal total = (manoDeObra + repuestos) * (1 - cliente.ObtenerDescuento());

            ValorTotal = decimal.Round(total, 2);
            return ValorTotal;
        }

        /// <summary>
        /// Minutos de todos los servicios en jornadas, multiplicados por el factor de fila del cliente, más la espera
        /// del repuesto más lento. Cuenta solo días hábiles (nunca domingo ni festivo).
        /// Si todo son ajustes rápidos no hace fila: se entrega el mismo día.
        /// </summary>
        public DateTime CalcularFechaPrometida(Cliente cliente, ICalendarioLaboral calendario)
        {
            int diasDeEjecucion = 0;
            if (_lineas.Any(l => l.Servicio.EntraAFila))
            {
                int minutos = _lineas.Sum(l => l.Servicio.MinutosEstimados);
                double jornadas = (double)minutos / calendario.MinutosProductivosPorDia * cliente.FactorFila;
                diasDeEjecucion = (int)Math.Ceiling(jornadas);
            }

            FechaEntregaPrometida = calendario.SumarDiasHabiles(FechaIngreso, diasDeEjecucion + DiasEsperaDeRepuestos());
            return FechaEntregaPrometida.Value;
        }

        /// <summary>Espera del repuesto más lento entre los que hay que pedir (los pedidos llegan en paralelo).</summary>
        public int DiasEsperaDeRepuestos()
            => _lineas.SelectMany(l => l.Repuestos)
                      .Where(r => r.EstaPendienteDeStock)
                      .Select(r => r.Repuesto.DiasEspera)
                      .DefaultIfEmpty(0)
                      .Max();

        public bool CotizacionVencida(ICalendarioLaboral calendario, DateTime ahora)
            => Estado == EstadoOrden.Cotizada
               && FechaCotizacion.HasValue
               && calendario.DiasHabilesEntre(FechaCotizacion.Value, ahora) >= DiasHabilesLimiteAutorizacion;

        // ---------------------------------------------------------------- interno

        private void CambiarEstado(EstadoOrden nuevo)
        {
            if (!Transiciones[Estado].Contains(nuevo))
                throw new ConflictoException($"Una orden en estado {Estado} no puede pasar a {nuevo}.");
            Estado = nuevo;
        }
    }
}
