using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microservicios.Atracciones.GraphQL.Types;

namespace Microservicios.Atracciones.GraphQL.Clients;

public class CatalogClient
{
    private readonly HttpClient _httpClient;

    public CatalogClient(HttpClient httpClient, IConfiguration configuration)
    {
        var baseUrl = configuration["ServiceUrls:Catalog"] ?? "http://catalog-api:8080";
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<List<AttractionDto>> GetAttractionsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CatalogResponse>("catalog/attraction");
            if (response?.Data?.Items == null) return [];

            return response.Data.Items.Select(item => new AttractionDto
            {
                Id = item.Id,
                Nombre = item.Name,
                Descripcion = item.DescriptionShort,
                Precio = item.StartingPrice ?? 0,
                Moneda = item.CurrencyCode,
                Ubicacion = item.LocationName,
                ImagenUrl = item.MainImageUrl,
                Slug = item.Slug
            }).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<AttractionDto?> GetAttractionByIdAsync(Guid id)
    {
        try
        {
            var attractions = await GetAttractionsAsync();
            return attractions.FirstOrDefault(a => a.Id == id);
        }
        catch
        {
            return null;
        }
    }

    private class CatalogResponse
    {
        public bool Success { get; set; }
        public CatalogData? Data { get; set; }
    }

    private class CatalogData
    {
        public List<CatalogAttractionDto> Items { get; set; } = [];
    }

    private class CatalogAttractionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DescriptionShort { get; set; }
        public decimal? StartingPrice { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public string LocationName { get; set; } = string.Empty;
        public string? MainImageUrl { get; set; }
        public string Slug { get; set; } = string.Empty;
    }
}

public class BookingClient
{
    private readonly HttpClient _httpClient;

    public BookingClient(HttpClient httpClient, IConfiguration configuration)
    {
        var baseUrl = configuration["ServiceUrls:Booking"] ?? "http://booking-api:8080";
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<List<BookingDto>> GetBookingsAsync(string token)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            bool isAdminOrPartner = false;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);
                    var roles = jwtToken.Claims
                        .Where(c => c.Type == "role" || c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList();
                    
                    isAdminOrPartner = roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || 
                                                     r.Equals("Partner", StringComparison.OrdinalIgnoreCase));
                }
            }
            catch
            {
                // Fallback a falso si hay error decodificando
            }

            string endpoint = isAdminOrPartner ? "admin-booking/management" : "admin-booking/user/history";
            var response = await _httpClient.GetFromJsonAsync<BookingSearchResponse>(endpoint);
            return response?.Data?.Items ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<BookingDto?> GetBookingByIdAsync(Guid id, string token)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.GetFromJsonAsync<BookingDetailWrapper>($"admin-booking/detail/{id}");
            return response?.Data;
        }
        catch
        {
            return null;
        }
    }

    private class BookingSearchResponse
    {
        public bool Success { get; set; }
        public PagedBookingData? Data { get; set; }
    }

    private class PagedBookingData
    {
        public List<BookingDto> Items { get; set; } = [];
    }

    private class BookingDetailWrapper
    {
        public bool Success { get; set; }
        public BookingDto? Data { get; set; }
    }
}

public class BillingClient
{
    private readonly HttpClient _httpClient;

    public BillingClient(HttpClient httpClient, IConfiguration configuration)
    {
        var baseUrl = configuration["ServiceUrls:Billing"] ?? "http://billing-api:8080";
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync(string token)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.GetFromJsonAsync<BillingSearchResponse>("billing/management");
            return response?.Items ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<InvoiceDto?> GetInvoiceByBookingIdAsync(Guid bookingId, string token)
    {
        try
        {
            var invoices = await GetInvoicesAsync(token);
            return invoices.FirstOrDefault(i => i.BookingId == bookingId);
        }
        catch
        {
            return null;
        }
    }

    private class BillingSearchResponse
    {
        public List<InvoiceDto> Items { get; set; } = [];
    }
}
