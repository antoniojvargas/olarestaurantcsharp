using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System;

namespace RestaurantApi.Models;

public class OrderItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Description { get; set; } = "";

    [Required]
    public int Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    [ForeignKey("Order")]
    public Guid OrderId { get; set; }

    [JsonIgnore]
    public Order? Order { get; set; }

    [NotMapped]
    public decimal Total => Quantity * UnitPrice;
}
