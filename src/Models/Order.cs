using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace RestaurantApi.Models;

public class Order
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string ClientName { get; set; } = "";

    [Required]
    public string Status { get; set; } = "initiated"; // initiated → sent → delivered

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relación uno-a-muchos con OrderItem
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
