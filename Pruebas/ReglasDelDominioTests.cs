using Business.Calendario;
using Models.Entidades;
using Models.Enumeraciones;
using Models.Excepciones;

namespace Pruebas
{
    /// <summary>Reproduce los ejemplos resueltos del documento de dominio y las reglas de bloqueo, estados y garantía.</summary>
    public class ReglasDelDominioTests
    {
        // Lunes 19 de octubre de 2026: sin festivos en las dos semanas siguientes.
        private static readonly DateTime Lunes = new(2026, 10, 19, 8, 0, 0);
        private readonly CalendarioColombia _calendario = new();

        private static Mecanico MecanicoGeneral() => new(1, "Mecánico General", NivelMecanico.General, 60000m);
        private static Cliente Particular() => new ClienteParticular(1, "100", "Juan Pérez", "300", "juan@correo.com");
        private static Cliente Flota() => new ClienteFlota(2, "900", "Flota SAS", "300", "flota@correo.com", 5000000m, 800000m);
        private static OrdenDeTrabajo NuevaOrden() => new(1, 1, 1, 12000, Lunes);

        private static Preventivo Prev() => new("PRV", "Preventivo", "Aceite y filtros", 60, NivelMecanico.General, 3000);
        private static Correctivo CrearCorrectivo(string codigo, int minutos, NivelMecanico nivel = NivelMecanico.General)
            => new(codigo, "Correctivo " + codigo, "Reparación de una falla", minutos, nivel);
        private static AjusteRapido Ajuste() => new("AJU", "Calibrar cadena", "Tensar la cadena", 20, NivelMecanico.Aprendiz);
        private static Repuesto CrearRepuesto(string codigo, decimal venta, int stock = 10, OrigenRepuesto origen = OrigenRepuesto.BodegaLocal)
            => new(codigo, codigo, venta / 2, venta, stock, 1, 0, origen);

        private static LineaServicio Linea(int id, Servicio servicio, params (Repuesto repuesto, int cantidad)[] repuestos)
        {
            var linea = new LineaServicio(id, servicio, MecanicoGeneral());
            foreach (var (repuesto, cantidad) in repuestos)
                linea.AgregarRepuesto(repuesto, cantidad);
            return linea;
        }

        // ------------------------------------------------------------------ valor de la orden

        [Theory]
        [InlineData(false, false, 145000)]  // Preventivo, particular
        [InlineData(true, false, 127600)]   // Preventivo, flota (12 %)
        [InlineData(false, true, 381000)]   // Tres servicios, particular (5 % solo sobre mano de obra)
        [InlineData(true, true, 335280)]    // Tres servicios, flota
        public void El_valor_coincide_con_los_ejemplos_del_documento(bool esFlota, bool tresServicios, int esperado)
        {
            var cliente = esFlota ? Flota() : Particular();
            var orden = NuevaOrden();
            orden.AgregarLinea(Linea(1, Prev(), (CrearRepuesto("ACE", 45000m), 1), (CrearRepuesto("FIL", 40000m), 1)));
            if (tresServicios)
            {
                orden.AgregarLinea(Linea(2, CrearCorrectivo("FRE", 60), (CrearRepuesto("PAS", 75000m), 1)));
                orden.AgregarLinea(Linea(3, CrearCorrectivo("ELE", 60), (CrearRepuesto("BOB", 50000m), 1)));
            }

            Assert.Equal((decimal)esperado, orden.CalcularValor(cliente));
        }

        // ------------------------------------------------------------------ fecha prometida

        [Fact]
        public void Ajuste_rapido_particular_se_entrega_el_mismo_dia()
        {
            var orden = NuevaOrden();
            orden.AgregarLinea(Linea(1, Ajuste()));
            Assert.Equal(Lunes.Date, orden.CalcularFechaPrometida(Particular(), _calendario).Date);
        }

        [Fact]
        public void Preventivo_particular_se_entrega_en_un_dia_habil()
        {
            var orden = NuevaOrden();
            orden.AgregarLinea(Linea(1, Prev()));
            Assert.Equal(new DateTime(2026, 10, 20), orden.CalcularFechaPrometida(Particular(), _calendario).Date);
        }

        [Fact]
        public void Correctivo_de_motor_particular_se_entrega_en_cuatro_dias_habiles()
        {
            var orden = NuevaOrden();
            orden.AgregarLinea(Linea(1, CrearCorrectivo("MOT", 960)));
            Assert.Equal(new DateTime(2026, 10, 23), orden.CalcularFechaPrometida(Particular(), _calendario).Date);
        }

        [Fact]
        public void El_mismo_correctivo_para_una_flota_entra_primero_y_se_entrega_en_dos_dias_habiles()
        {
            var orden = NuevaOrden();
            orden.AgregarLinea(Linea(1, CrearCorrectivo("MOT", 960)));
            Assert.Equal(new DateTime(2026, 10, 21), orden.CalcularFechaPrometida(Flota(), _calendario).Date);
        }

        [Fact]
        public void Correctivo_de_flota_con_repuesto_importado_suma_ocho_dias_de_espera_y_uno_de_ejecucion()
        {
            var orden = NuevaOrden();
            orden.AgregarLinea(Linea(1, CrearCorrectivo("MOT", 480),
                (CrearRepuesto("KIT", 320000m, stock: 0, origen: OrigenRepuesto.Importado), 1)));
            Assert.Equal(new DateTime(2026, 10, 29), orden.CalcularFechaPrometida(Flota(), _calendario).Date);
        }

        [Fact]
        public void La_fecha_nunca_cae_en_domingo_ni_festivo()
        {
            // Viernes 6 de noviembre + 3 días hábiles: sábado 7 (1), lunes 9 (2), martes 10 (3).
            Assert.Equal(new DateTime(2026, 11, 10), _calendario.SumarDiasHabiles(new DateTime(2026, 11, 6), 3));
            // Viernes 13 + 2: sábado 14 (1); el domingo y el lunes 16 (festivo trasladado) no cuentan; martes 17 (2).
            Assert.Equal(new DateTime(2026, 11, 17), _calendario.SumarDiasHabiles(new DateTime(2026, 11, 13), 2));
            Assert.False(_calendario.EsDiaHabil(new DateTime(2026, 11, 2)));
        }

        // ------------------------------------------------------------------ garantía

        [Fact]
        public void La_garantia_vence_por_tiempo_o_por_kilometraje_lo_que_ocurra_primero()
        {
            var garantia = new Garantia(1, 1, 1, new DateTime(2026, 10, 20), 30, 12000, 1000, "Preventivo");

            Assert.True(garantia.Validar(new DateTime(2026, 11, 10), 12500, 12000, false).Vigente);
            Assert.False(garantia.Validar(new DateTime(2026, 11, 25), 12500, 12000, false).Vigente); // por tiempo
            Assert.False(garantia.Validar(new DateTime(2026, 11, 10), 13100, 12000, false).Vigente); // por km
        }

        [Fact]
        public void La_garantia_se_pierde_por_otro_taller_o_por_kilometraje_inconsistente()
        {
            var garantia = new Garantia(1, 1, 1, new DateTime(2026, 10, 20), 30, 12000, 1000, "Preventivo");

            Assert.False(garantia.Validar(new DateTime(2026, 10, 25), 12100, 12000, intervenidaPorOtroTaller: true).Vigente);
            Assert.False(garantia.Validar(new DateTime(2026, 10, 25), 11000, 12000, false).Vigente);
        }

        // ------------------------------------------------------------------ reglas que impiden la operación

        [Fact]
        public void Un_cliente_de_convenio_necesita_numero_de_autorizacion_externo()
        {
            var convenio = new ClienteConvenio(3, "800", "Aseguradora", "300", "a@correo.com", 0.20m);

            Assert.Throws<ReglaNegocioException>(() => convenio.ValidarPuedeAbrirOrden(null));
            convenio.ValidarPuedeAbrirOrden("AUT-123"); // no lanza
        }

        [Fact]
        public void El_descuento_de_un_convenio_debe_estar_entre_15_y_25_por_ciento()
        {
            Assert.Throws<ReglaNegocioException>(() => new ClienteConvenio(3, "800", "Aseguradora", "300", "a@correo.com", 0.40m));
        }

        [Fact]
        public void Una_flota_sobre_su_cupo_y_con_facturas_vencidas_no_puede_abrir_orden()
        {
            var flota = new ClienteFlota(2, "900", "Flota SAS", "300", "f@correo.com", 1000000m, 500000m, tieneFacturasVencidas: true);
            flota.RegistrarCargo(1500000m);

            Assert.Throws<ReglaNegocioException>(() => flota.ValidarPuedeAbrirOrden(null));
        }

        [Fact]
        public void Un_aprendiz_no_puede_ejecutar_un_servicio_de_nivel_especialista()
        {
            var aprendiz = new Mecanico(3, "Aprendiz", NivelMecanico.Aprendiz, 25000m);
            Assert.Throws<ReglaNegocioException>(
                () => new LineaServicio(1, CrearCorrectivo("MOT", 960, NivelMecanico.Especialista), aprendiz));
        }

        [Fact]
        public void Un_ajuste_rapido_dura_menos_de_30_minutos()
        {
            Assert.Throws<ReglaNegocioException>(() => new AjusteRapido("AJU", "Ajuste", "Demasiado largo", 45, NivelMecanico.Aprendiz));
        }

        // ------------------------------------------------------------------ stock

        [Fact]
        public void Descontar_baja_el_stock_y_no_permite_pasarse()
        {
            var repuesto = CrearRepuesto("ACE", 45000m, stock: 5);

            repuesto.Descontar(2);
            Assert.Equal(3, repuesto.CantidadStock);
            Assert.Throws<ConflictoException>(() => repuesto.Descontar(4));
        }

        // ------------------------------------------------------------------ flujo de estados

        [Fact]
        public void Una_orden_recien_recibida_no_puede_terminarse()
        {
            Assert.Throws<ConflictoException>(() => NuevaOrden().Terminar());
        }

        [Fact]
        public void Un_correctivo_exige_diagnostico_y_autorizacion_antes_de_ejecutarse()
        {
            var cliente = Particular();
            var orden = NuevaOrden();
            var linea = Linea(1, CrearCorrectivo("FRE", 60));
            orden.AgregarLinea(linea);

            Assert.Throws<ReglaNegocioException>(() => orden.Cotizar(cliente, _calendario, Lunes)); // sin diagnóstico

            linea.RegistrarDiagnostico("Pastillas desgastadas");
            orden.Cotizar(cliente, _calendario, Lunes);
            Assert.Equal(EstadoOrden.Cotizada, orden.Estado);                                      // espera al cliente

            Assert.Throws<ConflictoException>(() => orden.IniciarEjecucion(Lunes));
            orden.Autorizar(_calendario, Lunes);
            orden.IniciarEjecucion(Lunes);
            orden.Terminar();
            orden.RegistrarEntrega(Lunes.AddDays(1));

            Assert.Equal(EstadoOrden.Entregada, orden.Estado);
            Assert.Single(linea.Garantias);
        }

        [Fact]
        {
            var orden = NuevaOrden();
        public void Un_preventivo_no_necesita_autorizacion_del_cliente()
            orden.AgregarLinea(Linea(1, Prev()));

            orden.Cotizar(Particular(), _calendario, Lunes);

            Assert.Equal(EstadoOrden.Autorizada, orden.Estado);
        }

        [Fact]
        public void Una_falla_adicional_en_ejecucion_devuelve_la_orden_a_cotizada()
        {
            var orden = NuevaOrden();
            orden.AgregarLinea(Linea(1, Prev()));
            orden.Cotizar(Particular(), _calendario, Lunes);
            orden.IniciarEjecucion(Lunes);

            orden.AgregarLinea(Linea(2, CrearCorrectivo("FRE", 60)));

            Assert.Equal(EstadoOrden.Cotizada, orden.Estado);
        }

        [Fact]
        public void Una_cotizacion_con_cinco_dias_habiles_sin_autorizar_esta_vencida()
        {
            var cliente = Particular();
            var orden = NuevaOrden();
            var linea = Linea(1, CrearCorrectivo("FRE", 60));
            linea.RegistrarDiagnostico("Pastillas desgastadas");
            orden.AgregarLinea(linea);
            orden.Cotizar(cliente, _calendario, Lunes);

            Assert.False(orden.CotizacionVencida(_calendario, Lunes.AddDays(3)));
            Assert.True(orden.CotizacionVencida(_calendario, Lunes.AddDays(6))); // domingo: ya pasaron 5 días hábiles
            Assert.Throws<ReglaNegocioException>(() => orden.Autorizar(_calendario, Lunes.AddDays(6)));
        }
    }
}
