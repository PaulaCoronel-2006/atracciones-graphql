using HotChocolate;
using HotChocolate.Types;
using Microservicios.Atracciones.GraphQL.Clients;
using Microservicios.Atracciones.GraphQL.Types;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Microservicios.Atracciones.GraphQL;

public class Query
{
    public async Task<List<BookingDto>> GetBookings(
        [Service] BookingClient client,
        HttpContext httpContext)
    {
        var token = GetToken(httpContext);
        return await client.GetBookingsAsync(token);
    }

    public async Task<BookingDto?> GetBooking(
        Guid id,
        [Service] BookingClient client,
        HttpContext httpContext)
    {
        var token = GetToken(httpContext);
        return await client.GetBookingByIdAsync(id, token);
    }

    public async Task<List<AttractionDto>> GetAttractions([Service] CatalogClient client)
    {
        return await client.GetAttractionsAsync();
    }

    public async Task<List<InvoiceDto>> GetInvoices(
        [Service] BillingClient client,
        HttpContext httpContext)
    {
        var token = GetToken(httpContext);
        return await client.GetInvoicesAsync(token);
    }

    private string GetToken(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }
        return string.Empty;
    }
}

public class BookingType : ObjectType<BookingDto>
{
    protected override void Configure(IObjectTypeDescriptor<BookingDto> descriptor)
    {
        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
        descriptor.Field(b => b.PnrCode).Type<StringType>();
        descriptor.Field(b => b.UserId).Type<IdType>();
        descriptor.Field(b => b.AttractionId).Type<IdType>();
        descriptor.Field(b => b.SlotId).Type<IdType>();
        descriptor.Field(b => b.StatusId).Type<ShortType>();
        descriptor.Field(b => b.TotalAmount).Type<DecimalType>();
        descriptor.Field(b => b.CurrencyCode).Type<StringType>();
        descriptor.Field(b => b.CreatedAt).Type<DateTimeType>();
        descriptor.Field(b => b.SlotDate).Type<StringType>();
        descriptor.Field(b => b.SlotStartTime).Type<StringType>();

        descriptor.Field<BookingResolvers>(r => r.GetAttraction(default!, default!))
            .Name("attraction");

        descriptor.Field<BookingResolvers>(r => r.GetInvoice(default!, default!, default!))
            .Name("invoice");
    }
}

public class BookingResolvers
{
    public async Task<AttractionDto?> GetAttraction(
        [Parent] BookingDto booking,
        [Service] CatalogClient client)
    {
        return await client.GetAttractionByIdAsync(booking.AttractionId);
    }

    public async Task<InvoiceDto?> GetInvoice(
        [Parent] BookingDto booking,
        [Service] BillingClient client,
        HttpContext httpContext)
    {
        var authHeader = httpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..].Trim()
            : string.Empty;

        return await client.GetInvoiceByBookingIdAsync(booking.Id, token);
    }
}
