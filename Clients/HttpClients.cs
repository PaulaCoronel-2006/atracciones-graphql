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
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = await _httpClient.GetFromJsonAsync<CatalogSearchResponse>("catalog/attraction", options);
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
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = await _httpClient.GetFromJsonAsync<CatalogDetailResponseWrapper>($"catalog/attraction/{id}", options);
            if (response?.Data == null) return null;

            var detail = response.Data;
            var mainImage = detail.Gallery.FirstOrDefault(g => g.IsMain)?.Url 
                         ?? detail.Gallery.FirstOrDefault()?.Url;

            decimal minPrice = 0;
            string currency = "USD";
            var prices = detail.Products.SelectMany(p => p.PriceTiers).ToList();
            if (prices.Any())
            {
                minPrice = prices.Min(p => p.Price);
                currency = prices.First().CurrencyCode;
            }

            return new AttractionDto
            {
                Id = detail.Id,
                Nombre = detail.Name,
                Descripcion = detail.DescriptionShort,
                Precio = minPrice,
                Moneda = currency,
                Ubicacion = detail.LocationName,
                ImagenUrl = mainImage,
                Slug = detail.Slug
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al consultar atracción por ID {id}: {ex.Message}");
            return null;
        }
    }

    private class CatalogSearchResponse
    {
        public bool Success { get; set; }
        public CatalogPagedResult? Data { get; set; }
    }

    private class CatalogDetailResponseWrapper
    {
        public bool Success { get; set; }
        public CatalogDetailResponseDto? Data { get; set; }
    }

    private class CatalogPagedResult
    {
        public List<CatalogAttractionDto> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }

    private class CatalogDetailResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DescriptionShort { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public List<CatalogMediaDto> Gallery { get; set; } = [];
        public List<CatalogProductDto> Products { get; set; } = [];
    }

    private class CatalogMediaDto
    {
        public string Url { get; set; } = string.Empty;
        public bool IsMain { get; set; }
    }

    private class CatalogProductDto
    {
        public List<CatalogPriceTierDto> PriceTiers { get; set; } = [];
    }

    private class CatalogPriceTierDto
    {
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; } = "USD";
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
