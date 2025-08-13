namespace EditSearch.Backend.Models.DTOs
{
    public class HotelForCreationDto
    {
        // From initial parsing
        public string ParsedName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // From Geoapify API
        public string? City { get; set; }
        public string? State { get; set; }
        public string? County { get; set; }
        public string? Postcode { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public string? FormattedAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // From Price Scraping
        public decimal? Price { get; set; }
    }
}