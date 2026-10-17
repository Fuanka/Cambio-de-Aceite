namespace Models.Interfaces
{
    /// <summary>
    /// Calendario del taller. La orden lo usa para calcular fechas sin conocer los festivos:
    /// la implementación (Colombia) vive en la capa de negocio.
    /// </summary>
    public interface ICalendarioLaboral
    {
        /// <summary>Minutos de trabajo efectivo que rinde una jornada.</summary>
        int MinutosProductivosPorDia { get; }

        /// <summary>Día hábil = lunes a sábado que no sea festivo.</summary>
        bool EsDiaHabil(DateTime fecha);

        /// <summary>Suma días hábiles. Con 0 días devuelve la misma fecha si es hábil, o el siguiente día hábil.</summary>
        DateTime SumarDiasHabiles(DateTime desde, int dias);

        /// <summary>Cuenta los días hábiles transcurridos después de 'desde' hasta 'hasta' (inclusive).</summary>
        int DiasHabilesEntre(DateTime desde, DateTime hasta);
    }
}
