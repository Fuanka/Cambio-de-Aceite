using Models.Enumeraciones;
using Models.Excepciones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Entidades
{
    public class Repuesto
    {
        // Días hábiles que tarda en llegar un repuesto que hay que pedir, según su origen.
        private static readonly Dictionary<OrigenRepuesto, int> DiasEsperaPorOrigen = new()
        {
            [OrigenRepuesto.BodegaLocal] = 1,
            [OrigenRepuesto.Medellin] = 3,
            [OrigenRepuesto.Importado] = 8
        };

        public string Codigo { get; }
        public string Nombre { get; }
        public decimal PrecioCosto { get; }
        public decimal PrecioVenta { get; }
        public int CantidadStock { get; private set; }
        public int StockMinimo { get; }
        public int UnidadesVendidasUltimoTrimestre { get; private set; }
        public OrigenRepuesto Origen { get; }
        public int DiasEspera => DiasEsperaPorOrigen[Origen];

        public Repuesto(string codigo, string nombre, decimal precioCosto, decimal precioVenta, int cantidadStock, int stockMinimo = 2, int unidadesVendidasUltimoTrimestre = 0, OrigenRepuesto origen = OrigenRepuesto.BodegaLocal)
        {
            if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre) || precioCosto < 0 || precioVenta < precioCosto || cantidadStock < 0 || stockMinimo < 0) 
                throw new ReglaNegocioException("Datos de repuesto invalidos.");

            Codigo = codigo; 
            Nombre = nombre; 
            PrecioCosto = precioCosto; 
            PrecioVenta = precioVenta; 
            CantidadStock = cantidadStock; 
            StockMinimo = stockMinimo; 
            UnidadesVendidasUltimoTrimestre = unidadesVendidasUltimoTrimestre;
            Origen = origen;
        }

        public bool HayExistencias(int cantidad) => cantidad > 0 && CantidadStock >= cantidad;

        public void Descontar(int cantidad)
        {
            if (cantidad <= 0)
                throw new ReglaNegocioException("La cantidad debe ser positiva");
            if (cantidad > CantidadStock)
                throw new ConflictoException($"Stock insuficiente de {Codigo}: se piden {cantidad} y hay {CantidadStock}.");

            CantidadStock -= cantidad;
            UnidadesVendidasUltimoTrimestre += cantidad;
        }

        public void Ingresar(int cantidad)
        {
            if (cantidad <= 0)
                throw new ReglaNegocioException("La cantidad debe ser positiva");

            CantidadStock += cantidad;
        }

        public bool BajoMinimo => CantidadStock < StockMinimo;
        public bool BajaRotacion => UnidadesVendidasUltimoTrimestre < 1;
    }
}
