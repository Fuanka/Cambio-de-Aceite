using Business.Interfaces;
using Models.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repositorios
{
    public class RepositorioOrdenesMemoria : IRepositorioOrdenes
    {
        private readonly List<OrdenDeTrabajo> _items = new();
        public IReadOnlyCollection<OrdenDeTrabajo> ObtenerTodos() => _items.AsReadOnly();
        public OrdenDeTrabajo? ObtenerPorId(int id) => _items.FirstOrDefault(x => x.Id == id);
        public int SiguienteId() => _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1;
        public void Agregar(OrdenDeTrabajo e) => _items.Add(e);
        public void Actualizar(OrdenDeTrabajo e) { } // con SQLite aquí va el UPDATE
    }
}
