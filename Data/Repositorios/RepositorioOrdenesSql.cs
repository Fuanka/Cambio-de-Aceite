using Business.Interfaces;
using Data.Sql;
using Microsoft.Data.SqlClient;
using Models.Entidades;
using Models.Enumeraciones;
using Models.Excepciones;

namespace Data.Repositorios
{
    /// <summary>
    /// Guarda la orden como un conjunto: la orden, sus líneas, los repuestos de cada línea y sus garantías.
    /// Todo se escribe en una sola transacción: o se guarda completo o no se guarda nada.
    /// </summary>
    public class RepositorioOrdenesSql(ConexionSql conexion, IRepositorioCatalogo catalogo) : IRepositorioOrdenes
    {
        // Filas tal como vienen de cada tabla, antes de armar los objetos.
        private sealed record FilaOrden(int Id, int ClienteId, int MotocicletaId, int KilometrajeIngreso, DateTime FechaIngreso,
            string? NumeroAutorizacionExterna, EstadoOrden Estado, decimal ValorTotal, DateTime? FechaCotizacion,
            DateTime? FechaEntregaPrometida, DateTime? FechaEntregaReal, bool CotizacionDesactualizada);

        private sealed record FilaLinea(int OrdenId, int LineaId, string ServicioCodigo, int MecanicoId, string Diagnostico,
            bool Autorizada, DateTime? FechaAutorizacion, DateTime? FechaEjecucion);

        private sealed record FilaUso(int OrdenId, int LineaId, string RepuestoCodigo, int Cantidad, decimal PrecioVentaUnitario, bool Descontado);

        private sealed record FilaGarantia(int OrdenId, int LineaId, int GarantiaId, DateTime FechaInicio, DateTime FechaVencimiento,
            int KilometrajeInicio, int KilometrajeLimite, string Condiciones, bool Anulada, string? MotivoAnulacion);

        private const string InsertarOrden =
            "INSERT INTO dbo.OrdenesTrabajo (Id, ClienteId, MotocicletaId, KilometrajeIngreso, FechaIngreso, NumeroAutorizacionExterna, " +
            "Estado, FechaCotizacion, FechaEntregaPrometida, FechaEntregaReal, ValorTotal, CotizacionDesactualizada) " +
            "VALUES (@Id, @ClienteId, @MotocicletaId, @KilometrajeIngreso, @FechaIngreso, @NumeroAutorizacionExterna, " +
            "@Estado, @FechaCotizacion, @FechaEntregaPrometida, @FechaEntregaReal, @ValorTotal, @CotizacionDesactualizada)";

        private const string ActualizarOrden =
            "UPDATE dbo.OrdenesTrabajo SET Estado = @Estado, FechaCotizacion = @FechaCotizacion, " +
            "FechaEntregaPrometida = @FechaEntregaPrometida, FechaEntregaReal = @FechaEntregaReal, " +
            "ValorTotal = @ValorTotal, CotizacionDesactualizada = @CotizacionDesactualizada WHERE Id = @Id";

        // ------------------------------------------------------------------ consultas

        public IReadOnlyCollection<OrdenDeTrabajo> ObtenerTodos() => Cargar(null);

        public OrdenDeTrabajo? ObtenerPorId(int id) => Cargar(id).FirstOrDefault();

        public int SiguienteId()
        {
            using var cn = conexion.Abrir();
            using var cmd = SqlComandos.Crear(cn, "SELECT NEXT VALUE FOR dbo.seq_Ordenes");
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public bool MotocicletaTieneOrdenAbierta(int motocicletaId)
        {
            const string sql =
                "SELECT COUNT(1) FROM dbo.OrdenesTrabajo WHERE MotocicletaId = @MotocicletaId " +
                "AND Estado <> N'Entregada' AND Estado <> N'CerradaSinEjecutar'";
            using var cn = conexion.Abrir();
            using var cmd = SqlComandos.Crear(cn, sql, null, ("@MotocicletaId", motocicletaId));
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        // ------------------------------------------------------------------ escritura

        public void Agregar(OrdenDeTrabajo orden)
        {
            try
            {
                Guardar(orden, esNueva: true);
            }
            catch (SqlException ex) when (SqlComandos.EsDuplicado(ex))
            {
                // El índice único de la base de datos impide dos órdenes abiertas para la misma moto.
                throw new ConflictoException("La motocicleta ya tiene una orden abierta sin cerrar.");
            }
        }

        public void Actualizar(OrdenDeTrabajo orden) => Guardar(orden, esNueva: false);

        private void Guardar(OrdenDeTrabajo orden, bool esNueva)
        {
            using var cn = conexion.Abrir();
            using var tx = cn.BeginTransaction();
            try
            {
                if (esNueva)
                {
                    Ejecutar(cn, tx, InsertarOrden, ParametrosOrden(orden));
                }
                else if (Ejecutar(cn, tx, ActualizarOrden, ParametrosOrden(orden)) == 0)
                {
                    throw new NoEncontradoException($"No existe la orden {orden.Id}.");
                }

                // Los hijos se reescriben completos: más simple y seguro que comparar qué cambió.
                Ejecutar(cn, tx, "DELETE FROM dbo.Garantias WHERE OrdenId = @OrdenId", ("@OrdenId", orden.Id));
                Ejecutar(cn, tx, "DELETE FROM dbo.UsosRepuesto WHERE OrdenId = @OrdenId", ("@OrdenId", orden.Id));
                Ejecutar(cn, tx, "DELETE FROM dbo.LineasServicio WHERE OrdenId = @OrdenId", ("@OrdenId", orden.Id));

                foreach (var linea in orden.Lineas)
                {
                    Ejecutar(cn, tx,
                        "INSERT INTO dbo.LineasServicio (OrdenId, LineaId, ServicioCodigo, MecanicoId, Diagnostico, Autorizada, FechaAutorizacion, FechaEjecucion) " +
                        "VALUES (@OrdenId, @LineaId, @ServicioCodigo, @MecanicoId, @Diagnostico, @Autorizada, @FechaAutorizacion, @FechaEjecucion)",
                        ("@OrdenId", orden.Id), ("@LineaId", linea.Id), ("@ServicioCodigo", linea.Servicio.Codigo),
                        ("@MecanicoId", linea.Mecanico.Id), ("@Diagnostico", linea.Diagnostico), ("@Autorizada", linea.Autorizada),
                        ("@FechaAutorizacion", linea.FechaAutorizacion), ("@FechaEjecucion", linea.FechaEjecucion));

                    int item = 0;
                    foreach (var uso in linea.Repuestos)
                    {
                        item++;
                        Ejecutar(cn, tx,
                            "INSERT INTO dbo.UsosRepuesto (OrdenId, LineaId, Item, RepuestoCodigo, Cantidad, PrecioVentaUnitario, Descontado) " +
                            "VALUES (@OrdenId, @LineaId, @Item, @RepuestoCodigo, @Cantidad, @PrecioVentaUnitario, @Descontado)",
                            ("@OrdenId", orden.Id), ("@LineaId", linea.Id), ("@Item", item), ("@RepuestoCodigo", uso.Repuesto.Codigo),
                            ("@Cantidad", uso.Cantidad), ("@PrecioVentaUnitario", uso.PrecioVentaUnitario), ("@Descontado", uso.Descontado));
                    }

                    foreach (var garantia in linea.Garantias)
                    {
                        Ejecutar(cn, tx,
                            "INSERT INTO dbo.Garantias (OrdenId, LineaId, GarantiaId, FechaInicio, FechaVencimiento, KilometrajeInicio, " +
                            "KilometrajeLimite, Condiciones, Anulada, MotivoAnulacion) " +
                            "VALUES (@OrdenId, @LineaId, @GarantiaId, @FechaInicio, @FechaVencimiento, @KilometrajeInicio, " +
                            "@KilometrajeLimite, @Condiciones, @Anulada, @MotivoAnulacion)",
                            ("@OrdenId", orden.Id), ("@LineaId", linea.Id), ("@GarantiaId", garantia.Id),
                            ("@FechaInicio", garantia.FechaInicio), ("@FechaVencimiento", garantia.FechaVencimiento),
                            ("@KilometrajeInicio", garantia.KilometrajeInicio), ("@KilometrajeLimite", garantia.KilometrajeLimite),
                            ("@Condiciones", garantia.Condiciones), ("@Anulada", garantia.Anulada),
                            ("@MotivoAnulacion", garantia.MotivoAnulacion));
                    }
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback(); // si algo falla a la mitad, no queda una orden a medio guardar
                throw;
            }
        }

        private static int Ejecutar(SqlConnection cn, SqlTransaction tx, string sql, params (string Nombre, object? Valor)[] parametros)
        {
            using var cmd = SqlComandos.Crear(cn, sql, tx, parametros);
            return cmd.ExecuteNonQuery();
        }

        private static (string, object?)[] ParametrosOrden(OrdenDeTrabajo o) =>
        [
            ("@Id", o.Id),
            ("@ClienteId", o.ClienteId),
            ("@MotocicletaId", o.MotocicletaId),
            ("@KilometrajeIngreso", o.KilometrajeIngreso),
            ("@FechaIngreso", o.FechaIngreso),
            ("@NumeroAutorizacionExterna", o.NumeroAutorizacionExterna),
            ("@Estado", o.Estado.ToString()),
            ("@FechaCotizacion", o.FechaCotizacion),
            ("@FechaEntregaPrometida", o.FechaEntregaPrometida),
            ("@FechaEntregaReal", o.FechaEntregaReal),
            ("@ValorTotal", o.ValorTotal),
            ("@CotizacionDesactualizada", o.CotizacionDesactualizada)
        ];

        // ------------------------------------------------------------------ lectura

        /// <summary>Trae una orden (o todas) con un solo viaje a la base de datos y arma los objetos.</summary>
        private List<OrdenDeTrabajo> Cargar(int? id)
        {
            // El catálogo se carga una vez: así dos líneas que usan el mismo repuesto comparten el mismo objeto.
            var servicios = catalogo.ObtenerServicios().ToDictionary(s => s.Codigo);
            var mecanicos = catalogo.ObtenerMecanicos().ToDictionary(m => m.Id);
            var repuestos = catalogo.ObtenerRepuestos().ToDictionary(r => r.Codigo);

            string filtroOrden = id.HasValue ? "WHERE Id = @Id " : string.Empty;
            string filtroHijos = id.HasValue ? "WHERE OrdenId = @Id " : string.Empty;

            string sql =
                "SELECT Id, ClienteId, MotocicletaId, KilometrajeIngreso, FechaIngreso, NumeroAutorizacionExterna, Estado, " +
                "ValorTotal, FechaCotizacion, FechaEntregaPrometida, FechaEntregaReal, CotizacionDesactualizada " +
                "FROM dbo.OrdenesTrabajo " + filtroOrden + "ORDER BY Id; " +
                "SELECT OrdenId, LineaId, ServicioCodigo, MecanicoId, Diagnostico, Autorizada, FechaAutorizacion, FechaEjecucion " +
                "FROM dbo.LineasServicio " + filtroHijos + "ORDER BY OrdenId, LineaId; " +
                "SELECT OrdenId, LineaId, RepuestoCodigo, Cantidad, PrecioVentaUnitario, Descontado " +
                "FROM dbo.UsosRepuesto " + filtroHijos + "ORDER BY OrdenId, LineaId, Item; " +
                "SELECT OrdenId, LineaId, GarantiaId, FechaInicio, FechaVencimiento, KilometrajeInicio, KilometrajeLimite, " +
                "Condiciones, Anulada, MotivoAnulacion " +
                "FROM dbo.Garantias " + filtroHijos + "ORDER BY OrdenId, LineaId, GarantiaId";

            var filasOrden = new List<FilaOrden>();
            var filasLinea = new List<FilaLinea>();
            var filasUso = new List<FilaUso>();
            var filasGarantia = new List<FilaGarantia>();

            using (var cn = conexion.Abrir())
            using (var cmd = SqlComandos.Crear(cn, sql, null, id.HasValue ? new (string, object?)[] { ("@Id", id.Value) } : Array.Empty<(string, object?)>()))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    filasOrden.Add(new FilaOrden(r.Entero("Id"), r.Entero("ClienteId"), r.Entero("MotocicletaId"),
                        r.Entero("KilometrajeIngreso"), r.Fecha("FechaIngreso"), r.TextoONulo("NumeroAutorizacionExterna"),
                        Enum.Parse<EstadoOrden>(r.Texto("Estado")), r.Decimal("ValorTotal"), r.FechaONula("FechaCotizacion"),
                        r.FechaONula("FechaEntregaPrometida"), r.FechaONula("FechaEntregaReal"), r.Booleano("CotizacionDesactualizada")));

                r.NextResult();
                while (r.Read())
                    filasLinea.Add(new FilaLinea(r.Entero("OrdenId"), r.Entero("LineaId"), r.Texto("ServicioCodigo"),
                        r.Entero("MecanicoId"), r.Texto("Diagnostico"), r.Booleano("Autorizada"),
                        r.FechaONula("FechaAutorizacion"), r.FechaONula("FechaEjecucion")));

                r.NextResult();
                while (r.Read())
                    filasUso.Add(new FilaUso(r.Entero("OrdenId"), r.Entero("LineaId"), r.Texto("RepuestoCodigo"),
                        r.Entero("Cantidad"), r.Decimal("PrecioVentaUnitario"), r.Booleano("Descontado")));

                r.NextResult();
                while (r.Read())
                    filasGarantia.Add(new FilaGarantia(r.Entero("OrdenId"), r.Entero("LineaId"), r.Entero("GarantiaId"),
                        r.Fecha("FechaInicio"), r.Fecha("FechaVencimiento"), r.Entero("KilometrajeInicio"),
                        r.Entero("KilometrajeLimite"), r.Texto("Condiciones"), r.Booleano("Anulada"), r.TextoONulo("MotivoAnulacion")));
            }

            var ordenes = new List<OrdenDeTrabajo>();
            foreach (var o in filasOrden)
            {
                var lineas = new List<LineaServicio>();
                foreach (var l in filasLinea.Where(x => x.OrdenId == o.Id))
                {
                    var usos = filasUso
                        .Where(u => u.OrdenId == l.OrdenId && u.LineaId == l.LineaId)
                        .Select(u => UsoRepuesto.Restaurar(repuestos[u.RepuestoCodigo], u.Cantidad, u.PrecioVentaUnitario, u.Descontado))
                        .ToList();

                    var garantias = filasGarantia
                        .Where(g => g.OrdenId == l.OrdenId && g.LineaId == l.LineaId)
                        .Select(RestaurarGarantia)
                        .ToList();

                    lineas.Add(LineaServicio.Restaurar(l.LineaId, servicios[l.ServicioCodigo], mecanicos[l.MecanicoId],
                        l.Diagnostico, l.Autorizada, l.FechaAutorizacion, l.FechaEjecucion, usos, garantias));
                }

                ordenes.Add(OrdenDeTrabajo.Restaurar(o.Id, o.ClienteId, o.MotocicletaId, o.KilometrajeIngreso, o.FechaIngreso,
                    o.NumeroAutorizacionExterna, o.Estado, o.ValorTotal, o.FechaCotizacion, o.FechaEntregaPrometida,
                    o.FechaEntregaReal, o.CotizacionDesactualizada, lineas));
            }
            return ordenes;
        }

        private static Garantia RestaurarGarantia(FilaGarantia g)
        {
            // La garantía se guardó con su fecha y su kilometraje de vencimiento; de ahí salen de nuevo los días y los km.
            int dias = (int)(g.FechaVencimiento - g.FechaInicio).TotalDays;
            int kilometros = g.KilometrajeLimite - g.KilometrajeInicio;
            var garantia = new Garantia(g.GarantiaId, g.OrdenId, g.LineaId, g.FechaInicio, dias, g.KilometrajeInicio, kilometros, g.Condiciones);
            if (g.Anulada)
                garantia.Anular(g.MotivoAnulacion ?? "Garantía anulada");
            return garantia;
        }
    }
}
