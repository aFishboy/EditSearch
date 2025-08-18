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

                // List<string> hotelList = await ParseHotelList.GetHotelListFromFile();

                // await GeoLocateHotel.GeoLocateHotelList(geoapifyApiKey, DB_FILE, context, hotelList);

                List<Hotel> hotelEntityList = context.Hotels.ToList();
                foreach (Hotel hotel in hotelEntityList)
                {
                    PriceScraper.ScrapeHotelPrice(hotel);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("", ex.Message);
            }
            finally
            {
                httpClient?.Dispose();
            }
        }

    }
}