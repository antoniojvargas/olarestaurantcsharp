using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Data;
using RestaurantApi.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;

    public OrdersController(AppDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // 1️⃣ Listar todas las órdenes (excepto delivered)
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        const string cacheKey = "orders:list";
        string? cached = await _cache.GetStringAsync(cacheKey);

        if (cached != null)
            return Ok(JsonSerializer.Deserialize<List<Order>>(cached));

        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.Status != "delivered")
            .ToListAsync();

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(orders),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            }
        );

        return Ok(orders);
    }

    // 2️⃣ Crear una nueva orden
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
  {
        order.Status = "initiated";
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Invalida la caché de la lista
        await _cache.RemoveAsync("orders:list");

        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    // 3️⃣ Avanzar el estado de una orden
    [HttpPost("{id}/advance")]
    public async Task<IActionResult> AdvanceOrder(int id)
    {
        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound();

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

        return Ok(order);
    }

    // 4️⃣ Ver detalle de una orden
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        string cacheKey = $"order:{id}";
        string? cached = await _cache.GetStringAsync(cacheKey);

        if (cached != null)
            return Ok(JsonSerializer.Deserialize<Order>(cached));

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(order),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            }
        );

        return Ok(order);
    }
}
