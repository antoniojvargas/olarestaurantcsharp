using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RestaurantApi.Models;

public class OrderItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string Description { get; set; } = "";

    [Required]
    public int Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    [ForeignKey("Order")]
    public int OrderId { get; set; }

    [JsonIgnore]
    public Order? Order { get; set; }

    [NotMapped]
    public decimal Total => Quantity * UnitPrice;
}
