using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EditSearch.Backend.Entities;

public class Hotel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    // This annotation explicitly links this collection to the 'Hotel'
    // navigation property in the HotelPrice class.
    [InverseProperty("Hotel")]
    public ICollection<HotelPrice> Prices { get; set; } = new List<HotelPrice>();
}