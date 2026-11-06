using Business.Interfaces;
using Data.Sql;
using Microsoft.Data.SqlClient;
using Models.Entidades;
using Models.Excepciones;

namespace Data.Repositorios
{
    public class RepositorioMotocicletasSql(ConexionSql conexion) : IRepositorioMotocicletas
    {
        private const string Seleccionar =
            "SELECT Id, Placa, Marca, Modelo, Cilindraje, Kilometraje, ClienteId FROM dbo.Motocicletas ";

        public IReadOnlyCollection<Motocicleta> ObtenerTodos()
        {
            using var cn = conexion.Abrir();
            using var cmd = SqlComandos.Crear(cn, Seleccionar + "ORDER BY Id");
            using var lector = cmd.ExecuteReader();
            var motos = new List<Motocicleta>();
            while (lector.Read())
                motos.Add(Mapear(lector));
            return motos;
        }

        public Motocicleta? ObtenerPorId(int id) => BuscarUna("WHERE Id = @Id", ("@Id", id));

        public Motocicleta? BuscarPorPlaca(string placa) => BuscarUna("WHERE Placa = @Placa", ("@Placa", placa));

        public int SiguienteId()
        {
            using var cn = conexion.Abrir();
            using var cmd = SqlComandos.Crear(cn, "SELECT NEXT VALUE FOR dbo.seq_Motocicletas");
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void Agregar(Motocicleta moto)
        {
            const string sql =
                "INSERT INTO dbo.Motocicletas (Id, Placa, Marca, Modelo, Cilindraje, Kilometraje, ClienteId) " +
                "VALUES (@Id, @Placa, @Marca, @Modelo, @Cilindraje, @Kilometraje, @ClienteId)";
            try
            {
                using var cn = conexion.Abrir();
                using var cmd = SqlComandos.Crear(cn, sql, null, Parametros(moto));
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex) when (SqlComandos.EsDuplicado(ex))
            {
                throw new ConflictoException("Ya existe una motocicleta con esa placa.");
            }
        }

        public void Actualizar(Motocicleta moto)
        {
            const string sql =
                "UPDATE dbo.Motocicletas SET Placa = @Placa, Marca = @Marca, Modelo = @Modelo, " +
                "Cilindraje = @Cilindraje, Kilometraje = @Kilometraje, ClienteId = @ClienteId WHERE Id = @Id";
            try
            {
                using var cn = conexion.Abrir();
                using var cmd = SqlComandos.Crear(cn, sql, null, Parametros(moto));
                if (cmd.ExecuteNonQuery() == 0)
                    throw new NoEncontradoException($"No existe la motocicleta {moto.Id}.");
            }
            catch (SqlException ex) when (SqlComandos.EsDuplicado(ex))
            {
                throw new ConflictoException("Ya existe una motocicleta con esa placa.");
            }
        }

        // ------------------------------------------------------------------ internos

        private Motocicleta? BuscarUna(string filtro, params (string Nombre, object? Valor)[] parametros)
        {
            using var cn = conexion.Abrir();
            using var cmd = SqlComandos.Crear(cn, Seleccionar + filtro, null, parametros);
            using var lector = cmd.ExecuteReader();
            return lector.Read() ? Mapear(lector) : null;
        }

        private static (string, object?)[] Parametros(Motocicleta m) =>
        [
            ("@Id", m.Id),
            ("@Placa", m.Placa),
            ("@Marca", m.Marca),
            ("@Modelo", m.Modelo),
            ("@Cilindraje", m.Cilindraje),
            ("@Kilometraje", m.Kilometraje),
            ("@ClienteId", m.ClienteId)
        ];

        private static Motocicleta Mapear(SqlDataReader r)
            => new(r.Entero("Id"), r.Texto("Placa"), r.Texto("Marca"), r.Texto("Modelo"),
                   r.Entero("Cilindraje"), r.Entero("Kilometraje"), r.Entero("ClienteId"));
    }
}
