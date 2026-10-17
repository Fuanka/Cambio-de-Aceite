using Models.Enumeraciones;

namespace Business.DTOs.Orden
{
    // ---------------------------------------------------------------- entrada
    public record CrearOrdenDto(
        int ClienteId,
        int MotocicletaId,
        int KilometrajeIngreso,
        List<LineaOrdenDto> Lineas,
        string? NumeroAutorizacionExterna = null);

    public record LineaOrdenDto(
        string CodigoServicio,
        int MecanicoId,
        string Diagnostico = "",
        List<RepuestoOrdenDto>? Repuestos = null);

    public record RepuestoOrdenDto(
        string Codigo,
        int Cantidad,
        bool ClienteAceptaEsperar = false);

    public record EntregarOrdenDto(int? KilometrajeEntrega = null);

    public record CerrarOrdenDto(string Motivo);

    // ---------------------------------------------------------------- salida
    public record UsoRepuestoDto(
        string Codigo, 
        string Nombre, 
        int Cantidad, 
        decimal PrecioVentaUnitario, 
        decimal Subtotal, 
        bool Consumido);

    public record GarantiaDto(
        int Id, 
        int LineaServicioId, 
        DateTime FechaInicio, 
        DateTime FechaVencimiento, 
        int KilometrajeLimite, 
        bool Anulada);

    public record LineaDto(
        int Id,
        string CodigoServicio,
        string NombreServicio,
        int MecanicoId,
        string NombreMecanico,
        string Diagnostico,
        bool Autorizada,
        decimal ManoDeObra,
        List<UsoRepuestoDto> Repuestos,
        List<GarantiaDto> Garantias);

    public record OrdenDto(
        int Id,
        int ClienteId,
        int MotocicletaId,
        int KilometrajeIngreso,
        DateTime FechaIngreso,
        DateTime? FechaCotizacion,
        DateTime? FechaEntregaPrometida,
        DateTime? FechaEntregaReal,
        EstadoOrden Estado,
        string? NumeroAutorizacionExterna,
        decimal ValorTotal,
        List<LineaDto> Lineas);

    public record EvaluacionGarantiaDto(
        int LineaServicioId, 
        string Servicio, 
        bool Vigente, 
        string Motivo);

    public record ConsultaGarantiaDto(
        int OrdenId, 
        DateTime FechaReclamo, 
        int KilometrajeActual, 
        List<EvaluacionGarantiaDto> Evaluaciones);
}
