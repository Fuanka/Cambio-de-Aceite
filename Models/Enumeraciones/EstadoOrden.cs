namespace Models.Enumeraciones
{
    /// <summary>Estados de la orden según el documento de dominio.</summary>
    public enum EstadoOrden
    {
        Recibida,
        EnDiagnostico,
        Cotizada,
        Autorizada,
        EnEjecucion,
        EnEsperaDeRepuesto,
        Terminada,
        Entregada,
        CerradaSinEjecutar
    }
}
