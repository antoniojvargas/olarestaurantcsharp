using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Data;
using RestaurantApi.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using RestaurantApi.Services;
using Serilog;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;

    private readonly OrderService _orderService;

    public OrdersController(AppDbContext context, IDistributedCache cache, OrderService orderService)
    {
        _context = context;
        _cache = cache;
        _orderService = orderService;
    }

    // 1️⃣ Listar todas las órdenes (excepto delivered)
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        Log.Information("GET /orders called");

        var orders = await _orderService.GetAllActiveOrdersAsync();
        return Ok(orders);
    }

    // 2️⃣ Crear una nueva orden
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
    {
        Log.Information("POST /orders called with payload: {@Order}", order);

        if (!ModelState.IsValid)
        {
            Log.Warning("Invalid order data received");
            return BadRequest(ModelState);
        }

        var createdOrder = await _orderService.CreateOrderAsync(order);
        return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, createdOrder);
    }

    // 3️⃣ Avanzar el estado de una orden
    [HttpPost("{id}/advance")]
    public async Task<IActionResult> AdvanceOrder(Guid id)
    {
        Log.Information("POST /orders/{Id}/advance called", id);

        var order = await _orderService.AdvanceOrderStatusAsync(id);
        if (order == null)
        {
            Log.Warning("Order with ID {Id} not found", id);
            return NotFound();
        }

        return Ok(order);
    }

    // 4️⃣ Ver detalle de una orden
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        Log.Information("GET /orders/{Id} called", id);

        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            Log.Warning("Order with ID {Id} not found", id);
            return NotFound();
        }

        return Ok(order);
    }
}
