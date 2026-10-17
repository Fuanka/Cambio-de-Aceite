namespace Business.Interfaces
{
    public interface IRepositorio<T> where T : class
    {
        IReadOnlyCollection<T> ObtenerTodos();
        T? ObtenerPorId(int id);

        void Agregar(T entidad);

        /// <summary>En memoria no hace falta (son las mismas referencias); con SQLite aquí se guardan los cambios.</summary>
        void Actualizar(T entidad);
    }
}
