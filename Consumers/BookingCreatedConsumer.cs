using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microservicios.Atracciones.Common.Events;
using Microservicios.Atracciones.GraphQL.Hubs;

namespace Microservicios.Atracciones.GraphQL.Consumers;

/// <summary>
/// Consumidor de MassTransit para recibir eventos BookingCreatedEvent
/// y hacer broadcast en tiempo real a través del NotificationsHub de SignalR.
/// </summary>
public class BookingCreatedConsumer : IConsumer<BookingCreatedEvent>
{
    private readonly IHubContext<NotificationsHub> _hubContext;

    public BookingCreatedConsumer(IHubContext<NotificationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Consume(ConsumeContext<BookingCreatedEvent> context)
    {
        var message = context.Message;
        
        // Broadcast de la notificación a todos los clientes de SignalR
        await _hubContext.Clients.All.SendAsync("BookingCreated", new
        {
            BookingId = message.BookingId,
            AttractionName = message.AttractionName,
            PnrCode = message.PnrCode,
            TotalAmount = message.TotalAmount,
            CreatedAt = message.CreatedAt
        });
    }
}
