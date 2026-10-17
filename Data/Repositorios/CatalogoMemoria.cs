using Business.Interfaces;
using Models.Entidades;
using Models.Enumeraciones;

namespace Data.Repositorios
{
    public class CatalogoMemoria : IRepositorioCatalogo
    {
        private readonly List<Servicio> _servicios =
        [
            new Preventivo("PREV-01", "Mantenimiento preventivo", "Aceite, filtros y revisión general", 90, NivelMecanico.General, 5000),
            new Correctivo("COR-01", "Reparación de frenos", "Revisión y reparación del sistema de frenos", 120, NivelMecanico.Especialista),
            new Correctivo("COR-02", "Reparación de motor", "Reparación mayor de motor", 960, NivelMecanico.Especialista),
            new AjusteRapido("AJU-01", "Ajuste rápido", "Ajuste menor sin garantía", 15, NivelMecanico.Aprendiz)
        ];

        private readonly List<Mecanico> _mecanicos =
        [
            new Mecanico(1, "Carlos Gómez", NivelMecanico.General, 40000),
            new Mecanico(2, "Ana Restrepo", NivelMecanico.Especialista, 60000),
            new Mecanico(3, "Luis Pérez", NivelMecanico.Aprendiz, 25000)
        ];

        private readonly List<Repuesto> _repuestos =
        [
            new Repuesto("ACE-01", "Aceite 4T", 18000, 25000, 30, 5, 20, OrigenRepuesto.BodegaLocal),
            new Repuesto("FIL-01", "Filtro de aceite", 9000, 14000, 2, 5, 4, OrigenRepuesto.Medellin),
            new Repuesto("PAS-01", "Pastillas de freno", 30000, 45000, 0, 3, 0, OrigenRepuesto.Importado)
        ];

        public IReadOnlyCollection<Servicio> ObtenerServicios() => _servicios.AsReadOnly();
        public Servicio? ObtenerServicio(string codigo) => _servicios.FirstOrDefault(x => x.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));
        public IReadOnlyCollection<Mecanico> ObtenerMecanicos() => _mecanicos.AsReadOnly();
        public Mecanico? ObtenerMecanico(int id) => _mecanicos.FirstOrDefault(x => x.Id == id);
        public IReadOnlyCollection<Repuesto> ObtenerRepuestos() => _repuestos.AsReadOnly();
        public Repuesto? ObtenerRepuesto(string codigo) => _repuestos.FirstOrDefault(x => x.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));

        public void ActualizarRepuesto(Repuesto repuesto) { } // con SQLite aquí va el UPDATE del stock
    }
}
