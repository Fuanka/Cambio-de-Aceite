using Business.Interfaces;
using Models.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repositorios
{
    public class RepositorioMotocicletasMemoria : IRepositorioMotocicletas
    {
        private readonly List<Motocicleta> _items = new();
        public IReadOnlyCollection<Motocicleta> ObtenerTodos() => _items.AsReadOnly();
        public Motocicleta? ObtenerPorId(int id) => _items.FirstOrDefault(x => x.Id == id);
        public Motocicleta? BuscarPorPlaca(string p) => _items.FirstOrDefault(x => x.Placa.Equals(p, StringComparison.OrdinalIgnoreCase));
        public int SiguienteId() => _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1;
        public void Agregar(Motocicleta e) => _items.Add(e);
        public void Actualizar(Motocicleta e) { } // con SQLite aquí va el UPDATE
    }
}
