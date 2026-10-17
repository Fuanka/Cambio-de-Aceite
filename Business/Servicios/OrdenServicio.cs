using Business.DTOs.Orden;
using Business.Interfaces;
using Business.Mapeos;
using Models.Entidades;
using Models.Enumeraciones;
using Models.Excepciones;
using Models.Interfaces;

namespace Business.Servicios
{
    /// <summary>
    /// Orquesta el flujo de una orden: busca lo que necesita, le pide a la entidad que aplique las reglas
    /// y guarda. Las reglas (descuentos, fechas, estados, garantías) viven en Models, no aquí.
    /// </summary>
    public class OrdenServicio(
        IRepositorioOrdenes ordenes,
        IRepositorioClientes clientes,
        IRepositorioMotocicletas motos,
        IRepositorioCatalogo catalogo,
        ICalendarioLaboral calendario) : IOrdenServicio
    {
        // La API atiende varias solicitudes a la vez y los datos viven en memoria compartida.
        private static readonly object Candado = new();

        // ------------------------------------------------------------------ consultas

        public IReadOnlyCollection<OrdenDto> Listar()
            => ordenes.ObtenerTodos().Select(o => Mapeador.ADto(o)).ToList();

        public OrdenDto Obtener(int id) => Mapeador.ADto(BuscarOrden(id));

        // ------------------------------------------------------------------ creación

        public OrdenDto Crear(CrearOrdenDto dto)
        {
            lock (Candado)
            {
                if (dto.Lineas is null || dto.Lineas.Count == 0)
                    throw new ReglaNegocioException("La orden necesita al menos un servicio.");

                var cliente = clientes.ObtenerPorId(dto.ClienteId)
                    ?? throw new NoEncontradoException($"No existe el cliente {dto.ClienteId}.");
                var moto = motos.ObtenerPorId(dto.MotocicletaId)
                    ?? throw new NoEncontradoException($"No existe la motocicleta {dto.MotocicletaId}.");

                if (moto.ClienteId != cliente.Id)
                    throw new ReglaNegocioException("La motocicleta no pertenece a ese cliente.");
                if (dto.KilometrajeIngreso < moto.Kilometraje)
                    throw new ReglaNegocioException("El kilometraje de ingreso no puede ser menor al último registrado.");
                if (ordenes.ObtenerTodos().Any(o => o.MotocicletaId == moto.Id && o.EstaAbierta))
                    throw new ConflictoException("La motocicleta ya tiene una orden abierta sin cerrar.");

                // Cada tipo de cliente decide qué lo bloquea (convenio sin número, flota sin cupo...).|
                cliente.ValidarPuedeAbrirOrden(dto.NumeroAutorizacionExterna);

                var ahora = DateTime.Now;
                var orden = new OrdenDeTrabajo(ordenes.SiguienteId(), cliente.Id, moto.Id,
                    dto.KilometrajeIngreso, ahora, dto.NumeroAutorizacionExterna);

                foreach (var l in dto.Lineas)
                {
                    var servicio = catalogo.ObtenerServicio(l.CodigoServicio)
                        ?? throw new NoEncontradoException($"No existe el servicio {l.CodigoServicio}.");
                    var mecanico = catalogo.ObtenerMecanico(l.MecanicoId)
                        ?? throw new NoEncontradoException($"No existe el mecánico {l.MecanicoId}.");

                    // El dominio valida que el mecánico esté disponible y tenga el nivel.
                    var linea = new LineaServicio(orden.Lineas.Count + 1, servicio, mecanico, l.Diagnostico);

                    foreach (var r in l.Repuestos ?? new List<RepuestoOrdenDto>())
                    {
                        var repuesto = catalogo.ObtenerRepuesto(r.Codigo)
                            ?? throw new NoEncontradoException($"No existe el repuesto {r.Codigo}.");

                        if (!repuesto.HayExistencias(r.Cantidad) && !r.ClienteAceptaEsperar)
                            throw new ReglaNegocioException(
                                $"El repuesto {repuesto.Codigo} no tiene existencias suficientes y el cliente no acepta esperar.");

                        linea.AgregarRepuesto(repuesto, r.Cantidad);
                    }

                    orden.AgregarLinea(linea);
                }

                // Calcula valor y fecha prometida; si no necesita autorización, queda Autorizada.
                orden.Cotizar(cliente, calendario, ahora);

                moto.ActualizarKilometraje(dto.KilometrajeIngreso);
                ordenes.Agregar(orden);
                motos.Actualizar(moto);
                return Mapeador.ADto(orden);
            }
        }

        // ------------------------------------------------------------------ flujo de estados

        public OrdenDto Autorizar(int id) => Ejecutar(id, orden =>
            orden.Autorizar(calendario, DateTime.Now));

        public OrdenDto IniciarEjecucion(int id) => Ejecutar(id, orden =>
        {
            orden.IniciarEjecucion(DateTime.Now);
            ConsumirRepuestosDisponibles(orden); // si falta alguno, la orden queda en espera de repuesto
        });

        public OrdenDto Reanudar(int id) => Ejecutar(id, orden =>
        {
            if (orden.Estado != EstadoOrden.EnEsperaDeRepuesto)
                throw new ConflictoException($"Una orden en estado {orden.Estado} no está esperando repuestos.");

            ConsumirRepuestosDisponibles(orden);
            if (orden.Estado == EstadoOrden.EnEsperaDeRepuesto)
                throw new ReglaNegocioException("Todavía faltan repuestos: registre su ingreso al inventario y vuelva a intentar.");
        });

        public OrdenDto Terminar(int id) => Ejecutar(id, orden => orden.Terminar());

        public OrdenDto Entregar(int id, int? kilometrajeEntrega) => Ejecutar(id, orden =>
        {
            var cliente = clientes.ObtenerPorId(orden.ClienteId)
                ?? throw new NoEncontradoException($"No existe el cliente {orden.ClienteId}.");
            var moto = motos.ObtenerPorId(orden.MotocicletaId)
                ?? throw new NoEncontradoException($"No existe la motocicleta {orden.MotocicletaId}.");

            orden.RegistrarEntrega(DateTime.Now, kilometrajeEntrega); // valida y genera las garantías
            moto.ActualizarKilometraje(kilometrajeEntrega ?? orden.KilometrajeIngreso);
            cliente.RegistrarCargo(orden.ValorTotal);

            motos.Actualizar(moto);
            clientes.Actualizar(cliente);
        });

        public OrdenDto CerrarSinEjecutar(int id, string motivo) => Ejecutar(id, orden =>
            orden.CerrarSinEjecutar(motivo));

        // ------------------------------------------------------------------ garantías

        public ConsultaGarantiaDto ConsultarGarantias(int id, int kilometrajeActual, DateTime? fechaReclamo, bool intervenidaPorOtroTaller)
        {
            var orden = BuscarOrden(id);
            if (orden.Estado != EstadoOrden.Entregada)
                throw new ReglaNegocioException("Solo las órdenes entregadas tienen garantía.");

            var moto = motos.ObtenerPorId(orden.MotocicletaId)
                ?? throw new NoEncontradoException($"No existe la motocicleta {orden.MotocicletaId}.");
            var fecha = fechaReclamo ?? DateTime.Now;

            var evaluaciones = new List<EvaluacionGarantiaDto>();
            foreach (var linea in orden.Lineas)
            {
                foreach (var garantia in linea.Garantias)
                {
                    var r = garantia.Validar(fecha, kilometrajeActual, moto.Kilometraje, intervenidaPorOtroTaller);
                    evaluaciones.Add(new EvaluacionGarantiaDto(linea.Id, linea.Servicio.Nombre, r.Vigente, r.Motivo));
                }
            }
            return new ConsultaGarantiaDto(orden.Id, fecha, kilometrajeActual, evaluaciones);
        }

        // ------------------------------------------------------------------ internos

        private OrdenDto Ejecutar(int id, Action<OrdenDeTrabajo> accion)
        {
            lock (Candado)
            {
                var orden = BuscarOrden(id);
                accion(orden);
                ordenes.Actualizar(orden);
                return Mapeador.ADto(orden);
            }
        }

        private OrdenDeTrabajo BuscarOrden(int id)
            => ordenes.ObtenerPorId(id) ?? throw new NoEncontradoException($"No existe la orden {id}.");

        /// <summary>Descuenta del inventario los repuestos que ya se pueden consumir y ajusta el estado de la orden.</summary>
        private void ConsumirRepuestosDisponibles(OrdenDeTrabajo orden)
        {
            foreach (var uso in orden.Lineas.SelectMany(l => l.Repuestos).Where(u => !u.Descontado))
            {
                if (uso.Repuesto.HayExistencias(uso.Cantidad))
                {
                    uso.Consumir();
                    catalogo.ActualizarRepuesto(uso.Repuesto);
                }
            }

            bool faltan = orden.Lineas.SelectMany(l => l.Repuestos).Any(u => !u.Descontado);
            if (faltan && orden.Estado == EstadoOrden.EnEjecucion)
                orden.MarcarEnEsperaDeRepuesto();
            else if (!faltan && orden.Estado == EstadoOrden.EnEsperaDeRepuesto)
                orden.Reanudar();
        }
    }
}
