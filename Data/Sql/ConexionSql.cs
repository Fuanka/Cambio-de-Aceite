using Microsoft.Data.SqlClient;

namespace Data.Sql
{
    /// <summary>
    /// Fábrica de conexiones a SQL Server. Cada operación abre su conexión, la usa y la cierra
    /// (con "using"); .NET reutiliza las conexiones físicas por debajo (connection pooling).
    /// </summary>
    public class ConexionSql(string cadenaConexion)
    {
        public SqlConnection Abrir()
        {
            var conexion = new SqlConnection(cadenaConexion);
            try
            {
                conexion.Open();
                return conexion;
            }
            catch
            {
                conexion.Dispose(); // si no abrió, no dejamos la conexión colgada
                throw;
            }
        }
    }
}
