using Business.DTOs.Cliente;
using Models.Entidades;
using Models.Enumeraciones;
using Models.Excepciones;

namespace Business.Fabricas
{
    /// <summary>
    /// Único lugar donde se decide qué subclase de Cliente crear según el tipo pedido.
    /// En el resto del sistema el cliente se usa de forma polimórfica.
    /// </summary>
    public static class FabricaClientes
    {
        public static Cliente Crear(int id, CrearClienteDto dto) => dto.Tipo switch
        {
            TipoCliente.Particular => new ClienteParticular(id, dto.Documento, dto.Nombre, dto.Telefono, dto.Correo),
            TipoCliente.Flota => new ClienteFlota(id, dto.Documento, dto.Nombre, dto.Telefono, dto.Correo,
                dto.CupoCredito, dto.MontoAutorizado, dto.TieneFacturasVencidas),
            TipoCliente.Convenio => new ClienteConvenio(id, dto.Documento, dto.Nombre, dto.Telefono, dto.Correo,
                dto.DescuentoConvenio ?? throw new ReglaNegocioException("El descuento del convenio es obligatorio (entre 0.15 y 0.25).")),
            _ => throw new ReglaNegocioException("Tipo de cliente no soportado.")
        };
    }
}
