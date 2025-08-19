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
using HotelDataImporter.GeoLocateHotel;

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
            if (geoapifyApiKey == null)
            {
                throw new Exception("API KEY IS NULL");
            }
            Console.WriteLine($"Successfully loaded API KEY: {geoapifyApiKey}");

            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlite($"Data Source={DB_FILE}");

                using var context = new AppDbContext(optionsBuilder.Options);


                List<string> hotelList = await ParseHotelList.GetHotelListFromFile();
                Console.WriteLine($"HotelList:");
                foreach (var hotel in hotelList)
                {
                    if (!context.Hotels.Any(h => hotel.Contains(h.Name)))
                    {
                        Console.WriteLine("", hotel);
                    }
                }

                // await GeoLocateHotel.GeoLocateHotelList(geoapifyApiKey, DB_FILE, context, hotelList);

                List<Hotel> hotelEntityList = context.Hotels.ToList();
                Console.WriteLine($"Found {hotelEntityList.Count} hotels to process");

                int successCount = 0;
                int failureCount = 0;

                foreach (Hotel hotel in hotelEntityList)
                {
                    Console.WriteLine($"\n=== Processing Hotel: {hotel.Name} in {hotel.City} ===");

                    try
                    {
                        // Call the new monthly price scraper
                        List<MonthlyPriceData> monthlyPrices = PriceScraper.ScrapeMonthlyHotelPrice(hotel);

                        if (monthlyPrices.Any())
                        {
                            // Save the prices to database
                            await SaveMonthlyPricesToDatabase(context, hotel.Id, monthlyPrices);

                            Console.WriteLine($"✅ SUCCESS: Saved {monthlyPrices.Count} monthly prices for {hotel.Name}");
                            successCount++;

                            // Show what we found
                            foreach (var price in monthlyPrices)
                            {
                                Console.WriteLine($"   {price.MonthName}: ${price.Price} ({price.Level})");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"❌ No prices found for {hotel.Name}");
                            failureCount++;
                        }

                        // Be respectful - add delay between hotels
                        Console.WriteLine("   Waiting 5 seconds before next hotel...");
                        await Task.Delay(5000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ ERROR processing {hotel.Name}: {ex.Message}");
                        failureCount++;

                        // Continue with next hotel even if this one fails
                        continue;
                    }
                }

                // Print final summary
                Console.WriteLine($"\n=== FINAL SUMMARY ===");
                Console.WriteLine($"Total Hotels Processed: {hotelEntityList.Count}");
                Console.WriteLine($"Successful: {successCount}");
                Console.WriteLine($"Failed: {failureCount}");
                Console.WriteLine($"Success Rate: {(double)successCount / hotelEntityList.Count * 100:F1}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                httpClient?.Dispose();
            }
        }

        private static async Task SaveMonthlyPricesToDatabase(AppDbContext context, int hotelId, List<MonthlyPriceData> monthlyPrices)
        {
            var currentYear = DateTime.UtcNow.Year;
            var nextYear = currentYear + 1;

            foreach (var monthlyPrice in monthlyPrices)
            {
                // Create a date for the middle of the target month next year
                var priceDate = new DateTime(nextYear, monthlyPrice.Month, 15);

                // Check if we already have a price entry for this hotel and month
                var existingPrice = await context.HotelPrice
                    .FirstOrDefaultAsync(p => p.HotelId == hotelId &&
                                             p.PriceDate.Year == nextYear &&
                                             p.PriceDate.Month == monthlyPrice.Month);

                if (existingPrice != null)
                {
                    // Update existing price
                    existingPrice.Price = monthlyPrice.Price;
                    existingPrice.Level = ConvertPriceLevel(monthlyPrice.Level);
                    existingPrice.LastUpdated = DateTime.UtcNow;
                    existingPrice.Source = "MonthlyScraperV2";
                    existingPrice.ConfidenceScore = 6; // Medium confidence for scraped data

                    Console.WriteLine($"   Updated existing price for {monthlyPrice.MonthName}");
                }
                else
                {
                    // Create new price entry
                    var hotelPrice = new HotelPrice
                    {
                        HotelId = hotelId,
                        Price = monthlyPrice.Price,
                        PriceDate = priceDate,
                        Level = ConvertPriceLevel(monthlyPrice.Level),
                        Source = "MonthlyScraperV2",
                        IsEstimated = true,
                        ConfidenceScore = 6,
                        Currency = "USD",
                        LastUpdated = DateTime.UtcNow
                    };

                    // Set the month and season automatically
                    hotelPrice.SetSeasonalData();

                    context.HotelPrice.Add(hotelPrice);
                    Console.WriteLine($"   Added new price for {monthlyPrice.MonthName}");
                }
            }

            // Save all changes to database
            await context.SaveChangesAsync();
        }

        // Convert the scraper's PriceLevel enum to the entity's PriceLevel enum
        private static EditSearch.Backend.Entities.PriceLevel ConvertPriceLevel(HotelDataImporter.PriceScraper.PriceLevel scraperLevel)
        {
            return scraperLevel switch
            {
                HotelDataImporter.PriceScraper.PriceLevel.Low => EditSearch.Backend.Entities.PriceLevel.Low,
                HotelDataImporter.PriceScraper.PriceLevel.Medium => EditSearch.Backend.Entities.PriceLevel.Medium,
                HotelDataImporter.PriceScraper.PriceLevel.High => EditSearch.Backend.Entities.PriceLevel.High,
                _ => EditSearch.Backend.Entities.PriceLevel.Medium
            };
        }
    }
}