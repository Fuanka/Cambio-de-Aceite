using Business.Interfaces;
using Data.Repositorios;
using Data.Sql;
using Microsoft.Extensions.DependencyInjection;

namespace Data
{
    public static class DataServiceCollectionExtensions
    {
        /// <summary>
        /// Registra los repositorios de SQL Server. No guardan datos en memoria (cada consulta va a la base),
        /// así que pueden ser singleton. El resto del sistema solo conoce las interfaces IRepositorio...
        /// </summary>
        public static IServiceCollection AddData(this IServiceCollection services, string cadenaConexion)
        {
            services.AddSingleton(new ConexionSql(cadenaConexion));
            services.AddSingleton<IRepositorioClientes, RepositorioClientesSql>();
            services.AddSingleton<IRepositorioMotocicletas, RepositorioMotocicletasSql>();
            services.AddSingleton<IRepositorioCatalogo, CatalogoSql>();
            services.AddSingleton<IRepositorioOrdenes, RepositorioOrdenesSql>();
            return services;
        }

        /// <summary>Versión sin base de datos (los datos se pierden al apagar). Útil para probar rápido.</summary>
        public static IServiceCollection AddDataEnMemoria(this IServiceCollection services)
        {
            services.AddSingleton<IRepositorioClientes, RepositorioClientesMemoria>();
            services.AddSingleton<IRepositorioMotocicletas, RepositorioMotocicletasMemoria>();
            services.AddSingleton<IRepositorioOrdenes, RepositorioOrdenesMemoria>();
            services.AddSingleton<IRepositorioCatalogo, CatalogoMemoria>();
            return services;
        }
    }
}
