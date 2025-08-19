using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EditSearch.Backend.Entities
{
    public enum PriceLevel
    {
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum Season
    {
        Winter = 1,  // Dec, Jan, Feb
        Spring = 2,  // Mar, Apr, May
        Summer = 3,  // Jun, Jul, Aug
        Fall = 4     // Sep, Oct, Nov
    }

    public class HotelPrice
    {
        public int Id { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public DateTime PriceDate { get; set; } // The date the price is for

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Enhanced seasonal tracking
        public int Month { get; set; } // 1-12, derived from PriceDate
        public Season Season { get; set; } // Derived from Month
        public PriceLevel Level { get; set; } = PriceLevel.Medium; // Default to medium

        // Source and reliability tracking
        [MaxLength(50)]
        public string Source { get; set; } = "Google"; // "Google", "Booking.com", "Manual", etc.

        public bool IsEstimated { get; set; } = true; // Most scraped prices are estimates

        // Confidence score (1-10, where 10 is most reliable)
        public int ConfidenceScore { get; set; } = 5;

        // Room type if available
        [MaxLength(100)]
        public string? RoomType { get; set; }

        // Currency (in case you expand internationally)
        [MaxLength(3)]
        public string Currency { get; set; } = "USD";

        // --- The Foreign Key Relationship ---
        public int HotelId { get; set; }

        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        // Helper method to automatically set Month and Season from PriceDate
        public void SetSeasonalData()
        {
            Month = PriceDate.Month;
            Season = Month switch
            {
                12 or 1 or 2 => Season.Winter,
                3 or 4 or 5 => Season.Spring,
                6 or 7 or 8 => Season.Summer,
                9 or 10 or 11 => Season.Fall,
                _ => Season.Spring
            };
        }
    }
}