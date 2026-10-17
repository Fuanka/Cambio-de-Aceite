using System.Collections.Concurrent;
using Models.Interfaces;

namespace Business.Calendario
{
    /// <summary>
    /// Se trabaja de lunes a sábado. Los domingos y los festivos de Colombia (incluidos los trasladados
    /// al lunes por la Ley Emiliani y los de Semana Santa) no son días hábiles.
    /// </summary>
    public class CalendarioColombia : ICalendarioLaboral
    {
        private static readonly ConcurrentDictionary<int, HashSet<DateTime>> FestivosPorAnio = new();

        public int MinutosProductivosPorDia => 480;

        public bool EsDiaHabil(DateTime fecha)
        {
            DateTime dia = fecha.Date;
            return dia.DayOfWeek != DayOfWeek.Sunday && !EsFestivo(dia);
        }

        public DateTime SumarDiasHabiles(DateTime desde, int dias)
        {
            if (dias < 0)
                throw new ArgumentOutOfRangeException(nameof(dias), "Los días a sumar no pueden ser negativos.");

            DateTime dia = desde.Date;
            if (dias == 0)
            {
                while (!EsDiaHabil(dia))
                    dia = dia.AddDays(1);
                return dia;
            }

            int restantes = dias;
            while (restantes > 0)
            {
                dia = dia.AddDays(1);
                if (EsDiaHabil(dia))
                    restantes--;
            }
            return dia;
        }

        public int DiasHabilesEntre(DateTime desde, DateTime hasta)
        {
            int contador = 0;
            for (DateTime dia = desde.Date.AddDays(1); dia <= hasta.Date; dia = dia.AddDays(1))
            {
                if (EsDiaHabil(dia))
                    contador++;
            }
            return contador;
        }

        private static bool EsFestivo(DateTime dia) => FestivosPorAnio.GetOrAdd(dia.Year, CalcularFestivos).Contains(dia);

        private static HashSet<DateTime> CalcularFestivos(int anio)
        {
            var festivos = new HashSet<DateTime>
            {
                new DateTime(anio, 1, 1),    // Año Nuevo
                new DateTime(anio, 5, 1),    // Día del Trabajo
                new DateTime(anio, 7, 20),   // Independencia
                new DateTime(anio, 8, 7),    // Batalla de Boyacá
                new DateTime(anio, 12, 8),   // Inmaculada Concepción
                new DateTime(anio, 12, 25)   // Navidad
            };

            // Festivos que se trasladan al lunes siguiente.
            var trasladables = new[] { (1, 6), (3, 19), (6, 29), (8, 15), (10, 12), (11, 1), (11, 11) };
            foreach (var (mes, dia) in trasladables)
                festivos.Add(ProximoLunes(new DateTime(anio, mes, dia)));

            DateTime pascua = DomingoDePascua(anio);
            festivos.Add(pascua.AddDays(-3));                // Jueves Santo
            festivos.Add(pascua.AddDays(-2));                // Viernes Santo
            festivos.Add(ProximoLunes(pascua.AddDays(39)));  // Ascensión del Señor
            festivos.Add(ProximoLunes(pascua.AddDays(60)));  // Corpus Christi
            festivos.Add(ProximoLunes(pascua.AddDays(68)));  // Sagrado Corazón
            return festivos;
        }

        private static DateTime ProximoLunes(DateTime fecha)
        {
            int dias = ((int)DayOfWeek.Monday - (int)fecha.DayOfWeek + 7) % 7;
            return fecha.AddDays(dias);
        }

        // Algoritmo de Meeus/Jones/Butcher.
        private static DateTime DomingoDePascua(int anio)
        {
            int a = anio % 19;
            int b = anio / 100;
            int c = anio % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int mes = (h + l - 7 * m + 114) / 31;
            int dia = ((h + l - 7 * m + 114) % 31) + 1;
            return new DateTime(anio, mes, dia);
        }
    }
}
