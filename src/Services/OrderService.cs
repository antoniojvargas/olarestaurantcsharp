using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RestaurantApi.Data;
using RestaurantApi.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RestaurantApi.Services
{
    public class OrderService
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<OrderService> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public OrderService(AppDbContext context, IDistributedCache cache, ILogger<OrderService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Order?> GetOrderByIdAsync(Guid id)
        {
            var cacheKey = $"order:{id}";

            // Intentar obtener desde caché
            var cachedOrder = await _cache.GetStringAsync(cacheKey);
            if (cachedOrder != null)
            {
                _logger.LogInformation("Cache HIT for {CacheKey}", cacheKey);
                return JsonSerializer.Deserialize<Order>(cachedOrder, _jsonOptions);
            }

            _logger.LogInformation("Cache MISS for {CacheKey}", cacheKey);

            // Si no está en caché, obtener desde DB
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(order, _jsonOptions),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });

                _logger.LogInformation("Cache SET for {CacheKey}", cacheKey);
            }

            return order;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
          _context.Orders.Add(order);
          await _context.SaveChangesAsync();

          _logger.LogInformation("Order {OrderId} created and added to database", order.Id);

          // Invalida el caché de esta orden
          var cacheKey = $"order:{order.Id}";
          await _cache.RemoveAsync(cacheKey);
          _logger.LogInformation("Cache INVALIDATED for {CacheKey}", cacheKey);

          return order;
        }
        
        public async Task<List<Order>> GetAllActiveOrdersAsync()
        {
            const string cacheKey = "orders:list";

            var cachedOrders = await _cache.GetStringAsync(cacheKey);
            if (cachedOrders != null)
            {
                _logger.LogInformation("Cache HIT for {CacheKey}", cacheKey);
                return JsonSerializer.Deserialize<List<Order>>(cachedOrders, _jsonOptions)!;
            }

            _logger.LogInformation("Cache MISS for {CacheKey}", cacheKey);

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Status != "delivered")
                .ToListAsync();

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(orders, _jsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });

            _logger.LogInformation("Cache SET for {CacheKey}", cacheKey);
            return orders;
        }


        public async Task<Order?> AdvanceOrderStatusAsync(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", id);
                return null;
            }

            var previousStatus = order.Status;
            order.Status = order.Status switch
            {
                "initiated" => "sent",
                "sent" => "delivered",
                _ => order.Status
            };

            if (order.Status == "delivered")
            {
                _context.Orders.Remove(order);
                await _cache.RemoveAsync($"order:{id}");
            }

            await _context.SaveChangesAsync();
            await _cache.RemoveAsync("orders:list");

            _logger.LogInformation("Order {OrderId} advanced from {OldStatus} to {NewStatus}",
                id, previousStatus, order.Status);

            return order;
        }

    }
}
