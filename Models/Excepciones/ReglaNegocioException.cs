namespace Models.Excepciones;

/// <summary>Una regla del negocio impide la operación (la API responde 422).</summary>
public class ReglaNegocioException(string message) : Exception(message) { }
