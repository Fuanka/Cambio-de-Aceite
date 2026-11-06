using System.Data;
using Microsoft.Data.SqlClient;

namespace Data.Sql
{
    internal static class SqlComandos
    {
        /// <summary>
        /// Crea un comando con PARÁMETROS (@Nombre). Los valores nunca se pegan dentro del texto SQL:
        /// así se evita la inyección de SQL. Un valor null se envía como NULL de la base de datos.
        /// </summary>
        public static SqlCommand Crear(SqlConnection conexion, string sql, SqlTransaction? transaccion = null,
                                       params (string Nombre, object? Valor)[] parametros)
        {
            var comando = conexion.CreateCommand();
            comando.CommandText = sql;
            if (transaccion is not null)
                comando.Transaction = transaccion;

            foreach (var (nombre, valor) in parametros)
            {
                SqlParameter parametro = valor is DateTime fecha
                    ? new SqlParameter(nombre, SqlDbType.DateTime2) { Value = fecha }
                    : new SqlParameter(nombre, valor ?? DBNull.Value);
                comando.Parameters.Add(parametro);
            }
            return comando;
        }

        /// <summary>2601 = índice único violado, 2627 = llave primaria o UNIQUE violada.</summary>
        public static bool EsDuplicado(SqlException ex) => ex.Number is 2601 or 2627;
    }

    /// <summary>Lectura de columnas por nombre, con y sin NULL.</summary>
    internal static class SqlLectorExtensiones
    {
        public static int Entero(this SqlDataReader r, string columna) => (int)r[columna];
        public static string Texto(this SqlDataReader r, string columna) => (string)r[columna];
        public static decimal Decimal(this SqlDataReader r, string columna) => (decimal)r[columna];
        public static bool Booleano(this SqlDataReader r, string columna) => (bool)r[columna];
        public static DateTime Fecha(this SqlDataReader r, string columna) => (DateTime)r[columna];

        public static string? TextoONulo(this SqlDataReader r, string columna) => r[columna] is DBNull ? null : (string)r[columna];
        public static int? EnteroONulo(this SqlDataReader r, string columna) => r[columna] is DBNull ? null : (int)r[columna];
        public static decimal? DecimalONulo(this SqlDataReader r, string columna) => r[columna] is DBNull ? null : (decimal)r[columna];
        public static bool? BooleanoONulo(this SqlDataReader r, string columna) => r[columna] is DBNull ? null : (bool)r[columna];
        public static DateTime? FechaONula(this SqlDataReader r, string columna) => r[columna] is DBNull ? null : (DateTime)r[columna];
    }
}
