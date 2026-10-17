using Business.Interfaces;
using Data.Repositorios;
using Microsoft.Extensions.DependencyInjection;

namespace Data
{
    public static class DataServiceCollectionExtensions
    {
        /// <summary>
        /// Registra los repositorios. Son singleton porque guardan los datos en memoria:
        /// al pasar a SQLite se cambian aquí las clases y nadie más se entera.
        /// </summary>
        public static IServiceCollection AddData(this IServiceCollection services)
        {
            services.AddSingleton<IRepositorioClientes, RepositorioClientesMemoria>();
            services.AddSingleton<IRepositorioMotocicletas, RepositorioMotocicletasMemoria>();
            services.AddSingleton<IRepositorioOrdenes, RepositorioOrdenesMemoria>();
            services.AddSingleton<IRepositorioCatalogo, CatalogoMemoria>();
            return services;
        }
    }
}
