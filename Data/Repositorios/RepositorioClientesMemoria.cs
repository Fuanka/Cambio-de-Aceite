using Business.Interfaces;
using Models.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repositorios
{
    public class RepositorioClientesMemoria : IRepositorioClientes
    {
        private readonly List<Cliente> _items = new();
        public IReadOnlyCollection<Cliente> ObtenerTodos() => _items.AsReadOnly();
        public Cliente? ObtenerPorId(int id) => _items.FirstOrDefault(x => x.Id == id);
        public Cliente? BuscarPorDocumento(string d) => _items.FirstOrDefault(x => x.Documento.Equals(d, StringComparison.OrdinalIgnoreCase));
        public int SiguienteId() => _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1;
        public void Agregar(Cliente e) => _items.Add(e);
        public void Actualizar(Cliente e) { } // en memoria son las mismas referencias; con SQLite aquí va el UPDATE
    }
}
