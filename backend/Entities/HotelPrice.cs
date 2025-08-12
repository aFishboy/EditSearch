using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EditSearch.Backend.Entities;

public class HotelPrice
{
    public int Id { get; set; }

    [Required]
    public decimal Price { get; set; }

    [Required]
    public DateTime PriceDate { get; set; } // The date the price is for

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // --- The Foreign Key Relationship ---

    // 1. The foreign key property itself
    public int HotelId { get; set; }

    // 2. The navigation property back to the parent Hotel
    // This annotation explicitly links the navigation property 'Hotel'
    // to the foreign key property 'HotelId'.
    [ForeignKey("HotelId")]
    public Hotel Hotel { get; set; }
}