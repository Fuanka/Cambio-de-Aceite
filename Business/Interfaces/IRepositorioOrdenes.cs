using Models.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Interfaces
{
    public interface IRepositorioOrdenes : IRepositorio<OrdenDeTrabajo>
    {
        int SiguienteId();
    }
}
