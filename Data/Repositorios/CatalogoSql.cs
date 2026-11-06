using Business.Interfaces;
using Data.Sql;
using Microsoft.Data.SqlClient;
using Models.Entidades;
using Models.Enumeraciones;
using Models.Excepciones;

namespace Data.Repositorios
{
    /// <summary>Servicios, mecánicos y repuestos. Los datos iniciales los carga el script de la base de datos.</summary>
    public class CatalogoSql(ConexionSql conexion) : IRepositorioCatalogo
    {
        private const string SeleccionarServicios =
            "SELECT Codigo, Tipo, Nombre, Descripcion, MinutosEstimados, NivelRequerido, KilometrajeObjetivo FROM dbo.Servicios ";
        private const string SeleccionarMecanicos =
            "SELECT Id, Nombre, Nivel, TarifaPorHora, Disponible FROM dbo.Mecanicos ";
        private const string SeleccionarRepuestos =
            "SELECT Codigo, Nombre, PrecioCosto, PrecioVenta, CantidadStock, StockMinimo, UnidadesVendidasUltimoTrimestre, Origen FROM dbo.Repuestos ";

        // ------------------------------------------------------------------ servicios

        public IReadOnlyCollection<Servicio> ObtenerServicios()
            => Listar(SeleccionarServicios + "ORDER BY Codigo", MapearServicio);

        public Servicio? ObtenerServicio(string codigo)
            => Listar(SeleccionarServicios + "WHERE Codigo = @Codigo", MapearServicio, ("@Codigo", codigo)).FirstOrDefault();

        // ------------------------------------------------------------------ mecánicos

        public IReadOnlyCollection<Mecanico> ObtenerMecanicos()
            => Listar(SeleccionarMecanicos + "ORDER BY Id", MapearMecanico);

        public Mecanico? ObtenerMecanico(int id)
            => Listar(SeleccionarMecanicos + "WHERE Id = @Id", MapearMecanico, ("@Id", id)).FirstOrDefault();

        // ------------------------------------------------------------------ repuestos

        public IReadOnlyCollection<Repuesto> ObtenerRepuestos()
            => Listar(SeleccionarRepuestos + "ORDER BY Codigo", MapearRepuesto);

        public Repuesto? ObtenerRepuesto(string codigo)
            => Listar(SeleccionarRepuestos + "WHERE Codigo = @Codigo", MapearRepuesto, ("@Codigo", codigo)).FirstOrDefault();

        public void ActualizarRepuesto(Repuesto repuesto)
        {
            const string sql =
                "UPDATE dbo.Repuestos SET CantidadStock = @CantidadStock, " +
                "UnidadesVendidasUltimoTrimestre = @Unidades WHERE Codigo = @Codigo";
            using var cn = conexion.Abrir();
            using var cmd = SqlComandos.Crear(cn, sql, null,
                ("@CantidadStock", repuesto.CantidadStock),
                ("@Unidades", repuesto.UnidadesVendidasUltimoTrimestre),
                ("@Codigo", repuesto.Codigo));
            if (cmd.ExecuteNonQuery() == 0)
                throw new NoEncontradoException($"No existe el repuesto {repuesto.Codigo}.");
        }

        // ------------------------------------------------------------------ internos

        private List<T> Listar<T>(string sql, Func<SqlDataReader, T> mapear, params (string Nombre, object? Valor)[] parametros)
        {
            using var cn = conexion.Abrir();
            using var cmd = SqlComandos.Crear(cn, sql, null, parametros);
            using var lector = cmd.ExecuteReader();
            var lista = new List<T>();
            while (lector.Read())
                lista.Add(mapear(lector));
            return lista;
        }

        private static Servicio MapearServicio(SqlDataReader r)
        {
            string codigo = r.Texto("Codigo");
            string nombre = r.Texto("Nombre");
            string descripcion = r.Texto("Descripcion");
            int minutos = r.Entero("MinutosEstimados");
            var nivel = Enum.Parse<NivelMecanico>(r.Texto("NivelRequerido"));

            // Se crea la subclase que corresponde a la familia guardada.
            return r.Texto("Tipo") switch
            {
                "Preventivo" => new Preventivo(codigo, nombre, descripcion, minutos, nivel, r.EnteroONulo("KilometrajeObjetivo") ?? 1),
                "Correctivo" => new Correctivo(codigo, nombre, descripcion, minutos, nivel),
                "AjusteRapido" => new AjusteRapido(codigo, nombre, descripcion, minutos, nivel),
                _ => throw new InvalidOperationException("Tipo de servicio desconocido en la base de datos.")
            };
        }

        private static Mecanico MapearMecanico(SqlDataReader r)
            => new(r.Entero("Id"), r.Texto("Nombre"), Enum.Parse<NivelMecanico>(r.Texto("Nivel")),
                   r.Decimal("TarifaPorHora"), r.Booleano("Disponible"));

        private static Repuesto MapearRepuesto(SqlDataReader r)
            => new(r.Texto("Codigo"), r.Texto("Nombre"), r.Decimal("PrecioCosto"), r.Decimal("PrecioVenta"),
                   r.Entero("CantidadStock"), r.Entero("StockMinimo"), r.Entero("UnidadesVendidasUltimoTrimestre"),
                   Enum.Parse<OrigenRepuesto>(r.Texto("Origen")));
    }
}
