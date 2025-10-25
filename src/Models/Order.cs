using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApi.Models;

public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string ClientName { get; set; } = "";

    [Required]
    public string Status { get; set; } = "initiated"; // initiated → sent → delivered

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relación uno-a-muchos con OrderItem
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
