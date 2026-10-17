using Models.Enumeraciones;

namespace Business.DTOs.Cliente
{
    // record: System.Text.Json puede llenarlo (con "private set" o solo "get" llegaría vacío desde Swagger).
    public record CrearClienteDto(
        string Documento,
        string Nombre,
        string Telefono,
        string Correo,
        TipoCliente Tipo,
        decimal CupoCredito = 0,
        decimal MontoAutorizado = 0,
        bool TieneFacturasVencidas = false,
        decimal? DescuentoConvenio = null);   // 0.15 a 0.25, solo para Convenio

    public record ClienteDto(
        int Id,
        string Documento,
        string Nombre,
        string Telefono,
        string Correo,
        TipoCliente Tipo,
        decimal Descuento,
        decimal? CupoCredito,
        decimal? MontoAutorizado,
        decimal? SaldoUtilizado,
        bool? TieneFacturasVencidas);
}
