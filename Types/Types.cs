namespace Microservicios.Atracciones.GraphQL.Types;

public record BookingDto
{
    public Guid Id { get; init; }
    public string PnrCode { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid AttractionId { get; init; }
    public Guid SlotId { get; init; }
    public short StatusId { get; init; }
    public decimal TotalAmount { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public DateTime CreatedAt { get; init; }
    public string? SlotDate { get; init; }
    public string? SlotStartTime { get; init; }
}

public record AttractionDto
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public decimal Precio { get; init; }
    public string Moneda { get; init; } = "USD";
    public string Ubicacion { get; init; } = string.Empty;
    public string? ImagenUrl { get; init; }
    public string Slug { get; init; } = string.Empty;
}

public record InvoiceDto
{
    public Guid Id { get; init; }
    public Guid BookingId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string TaxId { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime CreatedAt { get; init; }
}
