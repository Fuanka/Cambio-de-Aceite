using Business.Calendario;
using Business.Interfaces;
using Business.Servicios;
using Microsoft.Extensions.DependencyInjection;
using Models.Interfaces;

namespace Business
{
    public static class BusinessServiceCollectionExtensions
    {
        /// <summary>Registra los servicios de negocio y el calendario. La API solo llama a este método.</summary>
        public static IServiceCollection AddBusiness(this IServiceCollection services)
        {
            services.AddSingleton<ICalendarioLaboral, CalendarioColombia>();

            services.AddScoped<IClienteServicio, ClienteServicio>();
            services.AddScoped<IMotocicletaServicio, MotocicletaServicio>();
            services.AddScoped<IOrdenServicio, OrdenServicio>();
            services.AddScoped<ICatalogoServicio, CatalogoServicio>();
            return services;
        }
    }
}
