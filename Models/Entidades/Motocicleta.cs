using Models.Excepciones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Entidades
{
    public class Motocicleta
    {
        
        public int Id { get; private set; }
        public string Placa { get; private set; }
        public string Marca { get; private set; }
        public string Modelo { get; private set; }
        public int Cilindraje { get; private set; }
        public int Kilometraje { get; private set; }
        public int ClienteId { get; private set; }

        public Motocicleta(int id, string placa, string marca, string modelo, int cilindraje, int kilometraje, int clienteId)
        {
            if (string.IsNullOrWhiteSpace(placa) || string.IsNullOrWhiteSpace(marca)) 
                throw new ReglaNegocioException("placa y marca son obligatorias");

            if (cilindraje <= 0 ||  kilometraje < 0  || clienteId < 0) 
                throw new ReglaNegocioException("Cilindraje, kilometraje o cliente invalidos");

            Id = id;
            Placa = placa;
            Marca = marca;
            Modelo = modelo;
            Cilindraje = cilindraje;
            Kilometraje = kilometraje;
            ClienteId = clienteId;
        }

        public void ActualizarKilometraje(int kilometraje)
        {
            if (kilometraje<Kilometraje) 
                throw new ReglaNegocioException("El lilometraje no puede disminuir"); 
            Kilometraje=kilometraje;
        }

    }
}
