using Models.Enumeraciones;
using Models.Excepciones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Entidades
{
    public class Mecanico
    {
        public int Id { get; private set; }
        public string Nombre { get; private set; }
        public NivelMecanico Nivel { get; private set; }
        public decimal TarifaPorHora { get; private set; }
        public bool Disponible { get; private set; }


        public Mecanico(int id, string nombre, NivelMecanico nivel, decimal tarifaPorHora, bool disponible= true)
        {
            if (string.IsNullOrWhiteSpace(nombre) || tarifaPorHora < 0)
                throw new ReglaNegocioException("Nombre o tarifa invalidos");
            Id = id;
            Nombre = nombre;
            Nivel = nivel;
            TarifaPorHora = tarifaPorHora;
            Disponible = disponible;
        }

        public bool PuedeEjecutar(Servicio servicio)
        {
            return Disponible && (int)Nivel >= (int)servicio.NivelRequerido;
        }



    }
}
