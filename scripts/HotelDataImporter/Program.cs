using System.Text.Json;
using System.Text.RegularExpressions;
using EditSearch.Backend.Data;
using EditSearch.Backend.Entities;
using EditSearch.Backend.Models.DTOs;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HotelDataImporter.PriceScraper;
using HotelDataImporter.ParseHotelList;

namespace HotelScraper
{
    public class Program
    {
        private static readonly string DB_FILE = @"C:\Users\Sam\Coding\EditSearch\backend\editsearch.db";
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task Main(string[] args)
        {

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)             // Path where the JSON file exists
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true) // Optional dev overrides
                .AddEnvironmentVariables()                                 // Allow env var overrides
                .Build();

            // --- 2. RETRIEVE THE API KEY FROM CONFIGURATION ---
            // Use a colon ":" to access nested properties in the JSON file
            // 1. CONFIGURATION
            var geoapifyApiKey = config["ApiKeys:Geoapify"];
            Console.WriteLine($"Successfully loaded API KEY: {geoapifyApiKey}");

            // --- Your existing code continues here ---
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlite($"Data Source={DB_FILE}");

                using var context = new AppDbContext(optionsBuilder.Options);

                // Ensure the database is created based on your EF Core model
                // await context.Database.MigrateAsync();
                // Console.WriteLine("Database is ready and migrations are applied.");

                List<string> hotelList = await ParseHotelList.GetHotelListFromFile();

                // 8. PROCESS EACH HOTEL
                var pattern = new Regex(@"(.+?)\s+\((.+)\)");

                using var connection = new SqliteConnection($"Data Source={DB_FILE}");
                connection.Open();

                var existingPlaceIds = new HashSet<string>(context.Hotels.Select(h => h.PlaceID));
                int count = 1;
                foreach (var hotelInfoString in hotelList)
                {
                    Console.WriteLine("\n----------------Hotel Count " + count++ + "--------------------");

                    var newHotel = new Hotel { };
                    var match = pattern.Match(hotelInfoString);
                    if (!match.Success)
                    {
                        Console.WriteLine($"WARNING: Could not perform initial parse on: {hotelInfoString}");
                        continue;
                    }

                    // A. Parse the main parts
                    var parsedName = match.Groups[1].Value.Trim();
                    var locationString = match.Groups[2].Value.Trim();

                    if (parsedName.Contains(','))
                    {
                        var lastCommaIndex = parsedName.LastIndexOf(',');
                        parsedName = parsedName.Substring(0, lastCommaIndex).Trim();
                    }

                    // B. Split location string
                    var locationParts = locationString.Split(',').Select(p => p.Trim()).ToArray();

                    // C. Extract what we can
                    var parsedCountry = "N/A";
                    var knownCountryCodes = new[] { "US", "CA", "MX", "FR", "JP", "IE" };

                    foreach (var part in locationParts)
                    {
                        if (knownCountryCodes.Contains(part))
                        {
                            parsedCountry = part;
                            break;
                        }
                    }

                    Console.WriteLine($"Successfully Parsed: {parsedName} | Location: {locationString} | Country: {parsedCountry}");

                    // D. Geocoding
                    try
                    {
                        var searchText = $"{parsedName}, {locationString}";
                        var geoUrl = $"https://api.geoapify.com/v1/geocode/search?text={Uri.EscapeDataString(searchText)}&apiKey={geoapifyApiKey}";

                        Console.WriteLine($"  - Geocoding query: {geoUrl}");

                        var geoResponse = await httpClient.GetStringAsync(geoUrl);
                        string geoLogFilePath = "geoResponse.log";
                        string message = $"-------------GeoResponse: {geoResponse} ------------------\n";
                        File.AppendAllText(geoLogFilePath, message);
                        try
                        {
                            var geoData = JsonDocument.Parse(geoResponse);
                            var features = geoData.RootElement.GetProperty("features");

                            if (features.GetArrayLength() > 0)
                            {
                                // Inside your `if (features.GetArrayLength() > 0)` block:

                                var properties = features[0].GetProperty("properties");

                                // --- CORRECTED CODE ---
                                // Set the properties of the existing hotelDto object one by one
                                newHotel.Name = properties.TryGetProperty("name", out var nameElem)
                                    ? nameElem.GetString() ?? string.Empty
                                    : string.Empty;
                                newHotel.ParsedName = parsedName;
                                newHotel.PlaceID = properties.TryGetProperty("place_id", out var place_id) ? place_id.GetString() ?? string.Empty : string.Empty;
                                newHotel.City = properties.TryGetProperty("city", out var cityElem) ? cityElem.GetString() : null;
                                newHotel.State = properties.TryGetProperty("state", out var stateElem) ? stateElem.GetString() : null;
                                newHotel.County = properties.TryGetProperty("county", out var countyElem) ? countyElem.GetString() : null;
                                newHotel.Postcode = properties.TryGetProperty("postcode", out var postcodeElem) ? postcodeElem.GetString() : null;
                                newHotel.Country = properties.TryGetProperty("country", out var countryElem) ? countryElem.GetString() : null;
                                newHotel.CountryCode = properties.TryGetProperty("country_code", out var countryCodeElem) ? countryCodeElem.GetString() : null;
                                newHotel.FormattedAddress = properties.TryGetProperty("formatted", out var formattedElem) ? formattedElem.GetString() : null;
                                newHotel.Latitude = properties.TryGetProperty("lat", out var latElem) ? latElem.GetDouble() : null;
                                newHotel.Longitude = properties.TryGetProperty("lon", out var lonElem) ? lonElem.GetDouble() : null;

                                if (string.IsNullOrEmpty(newHotel.Name) || string.IsNullOrEmpty(newHotel.PlaceID))
                                {
                                    string logFilePath = "failed_imports.log";
                                    string errorMessage = $"---------Failed to process hotel. Hotel parsed name: {parsedName} ------------\n";
                                    errorMessage += $"{geoUrl}\n";
                                    File.AppendAllText(logFilePath, errorMessage);
                                    continue;
                                }

                                if (existingPlaceIds.Contains(newHotel.PlaceID))
                                {
                                    Console.WriteLine($"Skipping duplicate: {newHotel.Name}");
                                    continue;
                                }

                                existingPlaceIds.Add(newHotel.PlaceID);
                                context.Hotels.Add(newHotel);
                                Console.WriteLine($"  - Inserted '{newHotel.Name}'");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"  - JSON parsing or DTO creation failed: {e.Message}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"  - Geocoding failed: {e.Message}");
                    }

                    // var price = PriceScraper.ScrapeHotelPrice(newHotel); // NEED THIS TO GET THE PRICES !!!!!!!!!!!!!!!!

                }

                await context.SaveChangesAsync();
            }
            finally
            {
                httpClient?.Dispose();
            }
        }

    }
}