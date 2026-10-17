namespace Models.Excepciones;

/// <summary>No existe el cliente, la moto, la orden... que se busca (la API responde 404).</summary>
public class NoEncontradoException(string message) : ReglaNegocioException(message) { }
