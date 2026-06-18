using Microservicios.Atracciones.GraphQL;
using Microservicios.Atracciones.GraphQL.Clients;
using Microservicios.Atracciones.GraphQL.Hubs;
using Microservicios.Atracciones.GraphQL.Consumers;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Registrar SignalR
builder.Services.AddSignalR();

// Configurar MassTransit con RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<BookingCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitUri = builder.Configuration["RabbitMQ:Uri"];
        if (!string.IsNullOrEmpty(rabbitUri))
        {
            cfg.Host(new Uri(rabbitUri.Trim()));
        }
        else
        {
            var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
            var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
            var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(rabbitUser);
                h.Password(rabbitPass);
            });
        }

        cfg.ConfigureEndpoints(context);
    });
});

// Registrar clientes HTTP internos
builder.Services.AddHttpClient<CatalogClient>();
builder.Services.AddHttpClient<BookingClient>();
builder.Services.AddHttpClient<BillingClient>();

// Registrar HotChocolate GraphQL Server
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<BookingType>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.WithOrigins("https://atracciones-web.onrender.com", "http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

var app = builder.Build();

app.UseCors("AllowAll");

// Mapear la ruta /graphql
app.MapGraphQL();

// Mapear SignalR Hub
app.MapHub<NotificationsHub>("/hub/notifications");

app.Run();
