using Business.Interfaces;
using Data.Sql;
using Microsoft.Data.SqlClient;
using Models.Entidades;
using Models.Enumeraciones;
using Models.Excepciones;

namespace Data.Repositorios
{
    public class RepositorioClientesSql(ConexionSql conexion) : IRepositorioClientes
    {
        private const string Seleccionar =
            "SELECT Id, Documento, Nombre, Telefono, Correo, Tipo, CupoCredito, MontoAutorizado, " +
            "SaldoUtilizado, TieneFacturasVencidas, DescuentoNegociado FROM dbo.Clientes ";

        public IReadOnlyCollection<Cliente> ObtenerTodos()
        {
            using var cn = conexion.Abrir();
            using var cmd = SqlComandos.Crear(cn, Seleccionar + "ORDER BY Id");
            using var lector = cmd.ExecuteReader();
            var clientes = new List<Cliente>();
            while (lector.Read())
                clientes.Add(Mapear(lector));
            return clientes;
        }

        public Cliente? ObtenerPorId(int id) => BuscarUno("WHERE Id = @Id", ("@Id", id));

        public Cliente? BuscarPorDocumento(string documento) => BuscarUno("WHERE Documento = @Documento", ("@Documento", documento));

        public int SiguienteId()
        {
            using var cn = conexion.Abrir();
            using var cmd = SqlComandos.Crear(cn, "SELECT NEXT VALUE FOR dbo.seq_Clientes");
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void Agregar(Cliente cliente)
        {
            const string sql =
                "INSERT INTO dbo.Clientes (Id, Documento, Nombre, Telefono, Correo, Tipo, CupoCredito, MontoAutorizado, " +
                "SaldoUtilizado, TieneFacturasVencidas, DescuentoNegociado) " +
                "VALUES (@Id, @Documento, @Nombre, @Telefono, @Correo, @Tipo, @CupoCredito, @MontoAutorizado, " +
                "@SaldoUtilizado, @TieneFacturasVencidas, @DescuentoNegociado)";
            try
            {
                using var cn = conexion.Abrir();
                using var cmd = SqlComandos.Crear(cn, sql, null, Parametros(cliente));
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex) when (SqlComandos.EsDuplicado(ex))
            {
                throw new ConflictoException("Ya existe un cliente con ese documento.");
            }
        }

        public void Actualizar(Cliente cliente)
        {
            const string sql =
                "UPDATE dbo.Clientes SET Documento = @Documento, Nombre = @Nombre, Telefono = @Telefono, Correo = @Correo, " +
                "CupoCredito = @CupoCredito, MontoAutorizado = @MontoAutorizado, SaldoUtilizado = @SaldoUtilizado, " +
                "TieneFacturasVencidas = @TieneFacturasVencidas, DescuentoNegociado = @DescuentoNegociado " +
                "WHERE Id = @Id";
            try
            {
                using var cn = conexion.Abrir();
                using var cmd = SqlComandos.Crear(cn, sql, null, Parametros(cliente));
                if (cmd.ExecuteNonQuery() == 0)
                    throw new NoEncontradoException($"No existe el cliente {cliente.Id}.");
            }
            catch (SqlException ex) when (SqlComandos.EsDuplicado(ex))
            {
                throw new ConflictoException("Ya existe un cliente con ese documento.");
            }
        }

        // ------------------------------------------------------------------ internos

        private Cliente? BuscarUno(string filtro, params (string Nombre, object? Valor)[] parametros)
        {
            using var cn = conexion.Abrir();
            using var cmd = SqlComandos.Crear(cn, Seleccionar + filtro, null, parametros);
            using var lector = cmd.ExecuteReader();
            return lector.Read() ? Mapear(lector) : null;
        }

        // Los datos propios de cada tipo (cupo, descuento...) van en columnas que valen NULL para los demás.
        private static (string, object?)[] Parametros(Cliente c)
        {
            var flota = c as ClienteFlota;
            var convenio = c as ClienteConvenio;
            return
            [
                ("@Id", c.Id),
                ("@Documento", c.Documento),
                ("@Nombre", c.Nombre),
                ("@Telefono", c.Telefono),
                ("@Correo", c.Correo),
                ("@Tipo", c.Tipo.ToString()),
                ("@CupoCredito", flota?.CupoCredito),
                ("@MontoAutorizado", flota?.MontoAutorizado),
                ("@SaldoUtilizado", flota?.SaldoUtilizado),
                ("@TieneFacturasVencidas", flota?.TieneFacturasVencidas),
                ("@DescuentoNegociado", convenio?.DescuentoNegociado)
            ];
        }

        private static Cliente Mapear(SqlDataReader r)
        {
            int id = r.Entero("Id");
            string documento = r.Texto("Documento");
            string nombre = r.Texto("Nombre");
            string telefono = r.Texto("Telefono");
            string correo = r.Texto("Correo");

            // Se crea la subclase que corresponde al tipo guardado.
            Cliente cliente = Enum.Parse<TipoCliente>(r.Texto("Tipo")) switch
            {
                TipoCliente.Particular => new ClienteParticular(id, documento, nombre, telefono, correo),
                TipoCliente.Flota => new ClienteFlota(id, documento, nombre, telefono, correo,
                    r.DecimalONulo("CupoCredito") ?? 0m,
                    r.DecimalONulo("MontoAutorizado") ?? 0m,
                    r.BooleanoONulo("TieneFacturasVencidas") ?? false),
                TipoCliente.Convenio => new ClienteConvenio(id, documento, nombre, telefono, correo,
                    r.DecimalONulo("DescuentoNegociado") ?? ClienteConvenio.DescuentoMinimo),
                _ => throw new InvalidOperationException("Tipo de cliente desconocido en la base de datos.")
            };

            // El saldo se reconstruye con el mismo método que lo suma al entregar una orden.
            decimal saldo = r.DecimalONulo("SaldoUtilizado") ?? 0m;
            if (saldo > 0)
                cliente.RegistrarCargo(saldo);

            return cliente;
        }
    }
}
