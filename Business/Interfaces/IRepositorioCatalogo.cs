using Models.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Interfaces
{
    public interface IRepositorioCatalogo
    {
        IReadOnlyCollection<Servicio> ObtenerServicios();
        Servicio? ObtenerServicio(string codigo);
        IReadOnlyCollection<Mecanico> ObtenerMecanicos();
        Mecanico? ObtenerMecanico(int id);
        IReadOnlyCollection<Repuesto> ObtenerRepuestos();
        Repuesto? ObtenerRepuesto(string codigo);

        /// <summary>Guarda los cambios de stock de un repuesto (con SQLite hará el UPDATE).</summary>
        void ActualizarRepuesto(Repuesto repuesto);
    }
}
