namespace Models.Excepciones;

/// <summary>El estado actual impide la operación: duplicados, transición inválida, sin stock (la API responde 409).</summary>
public class ConflictoException(string message) : ReglaNegocioException(message) { }
