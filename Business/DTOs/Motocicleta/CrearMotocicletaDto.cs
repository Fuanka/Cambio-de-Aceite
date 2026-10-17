namespace Business.DTOs.Motocicleta
{
    public record CrearMotocicletaDto(
        string Placa,
        string Marca,
        string Modelo,
        int Cilindraje,
        int Kilometraje,
        int ClienteId);

    public record MotocicletaDto(
        int Id,
        string Placa,
        string Marca,
        string Modelo,
        int Cilindraje,
        int Kilometraje,
        int ClienteId);
}
