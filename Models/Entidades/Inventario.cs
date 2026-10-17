using Models.Excepciones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Entidades
{
    public class Inventario
    {
        private readonly Dictionary<string, Repuesto> _repuestos = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<Repuesto> Repuestos => _repuestos.Values;
        public void Agregar(Repuesto repuesto) 
        { 
            if (!_repuestos.TryAdd(repuesto.Codigo, repuesto)) 
                throw new ReglaNegocioException("Ya existe ese codigo de repuesto."); 
        }
        public Repuesto? Buscar(string codigo)
        {
           return _repuestos.GetValueOrDefault(codigo);
        }
        public void Descontar(string codigo, int cantidad) 
        { 
            var r = Buscar(codigo) ?? 
                throw new ReglaNegocioException("Repuesto no encontrado."); 
            r.Descontar(cantidad);
        }
        public decimal CalcularPlataQuieta() 
        {
            return _repuestos.Values.Where(r => r.BajaRotacion).Sum(r => r.PrecioCosto * r.CantidadStock);
        } 
    }
}
