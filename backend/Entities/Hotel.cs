using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace EditSearch.Backend.Entities
{
    public class Hotel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        public string ParsedName { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? State { get; set; } // For US/Canada, etc.
        public string? County { get; set; } // e.g., Kyoto Prefecture
        public string? Postcode { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public string? FormattedAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [InverseProperty("Hotel")]
        public ICollection<HotelPrice> Prices { get; set; } = new List<HotelPrice>();
    }
}