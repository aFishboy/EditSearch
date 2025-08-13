using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EditSearch.Backend.Data;
using EditSearch.Backend.Entities;
using EditSearch.Backend.Models.DTOs;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using dotenv.net;

namespace HotelScraper
{
    public class Program
    {
        private static readonly string DB_FILE = @"C:\Users\Sam\Coding\EditSearch\backend\editsearch.db";
        private static readonly HttpClient httpClient = new HttpClient();
        private static IWebDriver driver;

        public static async Task Main(string[] args)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlite($"Data Source={DB_FILE}");

                // We use a 'using' block to ensure the context is disposed of properly
                using var context = new AppDbContext(optionsBuilder.Options);

                // Ensure the database is created based on your EF Core model
                // await context.Database.MigrateAsync();
                // Console.WriteLine("Database is ready and migrations are applied.");

                // 1. CONFIGURATION
                DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 2));
                var geoapifyApiKey = Environment.GetEnvironmentVariable("GEOAPIFY_API_KEY");
                geoapifyApiKey = "17c3623af872482d90ea51f06662c7b7";
                Console.WriteLine($"API KEY: {geoapifyApiKey}");

                // Build reliable path to file
                var scriptDir = Directory.GetCurrentDirectory();
                var filePath = Path.Combine(scriptDir, "hotelListHtml.txt");

                // 2. SETUP DATABASE
                // SetupDatabase(DB_FILE);

                // --- 3. SETUP WEB SCRAPER WITH ANTI-DETECTION OPTIONS ---
                var options = new ChromeOptions();

                // 1. Set a realistic User-Agent
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");

                // 2. Disable the "Chrome is being controlled by automated test software" infobar
                options.AddExcludedArgument("enable-automation");

                // 3. Important: Disable features that can reveal automation
                options.AddArgument("--disable-blink-features=AutomationControlled");

                // Disable for debugging, enable for production runs
                // options.AddArgument("--headless");

                driver = new ChromeDriver(options);
                Console.WriteLine("Using stealth ChromeDriver.");

                // 4. GRAB DATA FROM FILE
                string longString;
                try
                {
                    longString = await File.ReadAllTextAsync(filePath);
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine("Error: The file 'hotelListHtml.txt' was not found in this directory.");
                    return;
                }

                // 5. SPLIT THE STRING INTO A LIST
                var delimiter = "</li><li>";
                var splitList = longString.Split(delimiter);

                // 6. CLEAN UP EACH ITEM IN THE LIST
                var cleanedHotels = new List<string>();
                foreach (var hotelString in splitList)
                {
                    var cleanItem = hotelString.Trim();
                    if (cleanItem.StartsWith("<li>"))
                        cleanItem = cleanItem.Substring(4);
                    if (cleanItem.EndsWith("</li>"))
                        cleanItem = cleanItem.Substring(0, cleanItem.Length - 5);

                    var finalItem = cleanItem.Split(new[] { " - " }, 2, StringSplitOptions.None)[0].Trim();
                    cleanedHotels.Add(finalItem);
                }

                // 7. FILTER BY SEARCH TERMS
                var searchTerms = new[] { "Japan", "JP", "Tokyo", "Kyoto", "Osaka" };
                var matchingHotels = cleanedHotels.Where(h => searchTerms.Any(term => h.Contains(term))).Take(2).ToList();

                foreach (var hotel in matchingHotels)
                {
                    Console.WriteLine(hotel);
                }
                Console.WriteLine($"Matching length: {matchingHotels.Count}.\n");

                // 8. PROCESS EACH HOTEL
                var pattern = new Regex(@"(.+?)\s+\((.+)\)");

                using var connection = new SqliteConnection($"Data Source={DB_FILE}");
                connection.Open();

                foreach (var hotelInfoString in matchingHotels)
                {
                    var hotelDto = new HotelForCreationDto { };
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

                        Console.WriteLine($"  - Geocoding query: {searchText}");

                        var geoResponse = await httpClient.GetStringAsync(geoUrl);
                        try
                        {
                            var geoData = JsonDocument.Parse(geoResponse);
                            var features = geoData.RootElement.GetProperty("features");
                            Console.WriteLine($"{features} features");

                            if (features.GetArrayLength() > 0)
                            {
                                // Inside your `if (features.GetArrayLength() > 0)` block:

                                var properties = features[0].GetProperty("properties");

                                // --- CORRECTED CODE ---
                                // Set the properties of the existing hotelDto object one by one
                                hotelDto.Name = properties.TryGetProperty("name", out var nameElem)
                                    ? nameElem.GetString() ?? parsedName
                                    : parsedName;
                                hotelDto.ParsedName = parsedName;
                                hotelDto.City = properties.TryGetProperty("city", out var cityElem) ? cityElem.GetString() : null;
                                hotelDto.State = properties.TryGetProperty("state", out var stateElem) ? stateElem.GetString() : null;
                                hotelDto.County = properties.TryGetProperty("county", out var countyElem) ? countyElem.GetString() : null;
                                hotelDto.Postcode = properties.TryGetProperty("postcode", out var postcodeElem) ? postcodeElem.GetString() : null;
                                hotelDto.Country = properties.TryGetProperty("country", out var countryElem) ? countryElem.GetString() : null;
                                hotelDto.CountryCode = properties.TryGetProperty("country_code", out var countryCodeElem) ? countryCodeElem.GetString() : null;
                                hotelDto.FormattedAddress = properties.TryGetProperty("formatted", out var formattedElem) ? formattedElem.GetString() : null;
                                hotelDto.Latitude = properties.TryGetProperty("lat", out var latElem) ? latElem.GetDouble() : null;
                                hotelDto.Longitude = properties.TryGetProperty("lon", out var lonElem) ? lonElem.GetDouble() : null;

                                // Now, use the DTO to create your database entity
                                var newHotel = new Hotel
                                {
                                    Name = hotelDto.Name,
                                    City = hotelDto.City,
                                    State = hotelDto.State,
                                    County = hotelDto.County,
                                    Postcode = hotelDto.Postcode,
                                    Country = hotelDto.Country,
                                    CountryCode = hotelDto.CountryCode,
                                    FormattedAddress = hotelDto.FormattedAddress,
                                    Latitude = hotelDto.Latitude,
                                    Longitude = hotelDto.Longitude
                                };

                                // ... Add the price to newHotel.Prices and save to DbContext ...
                                context.Hotels.Add(newHotel);
                                Console.WriteLine($"  - Staged '{newHotel.Name}' for insertion with full details.");
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

                    // E. Price scraping
                    string priceStr = null;
                    try
                    {
                        var query = $"{hotelDto.Name} {hotelDto.City} price";
                        Console.WriteLine($"https://www.google.com/search?q={Uri.EscapeDataString(query)}");
                        driver.Navigate().GoToUrl($"https://www.google.com/search?q={Uri.EscapeDataString(query)}");

                        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var selector = "div[aria-label*='View prices']";

                        var priceElement = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(selector)));
                        var fullLabel = priceElement.GetAttribute("aria-label");
                        Console.WriteLine($"  - Found label: '{fullLabel}'");

                        var priceMatch = Regex.Match(fullLabel, @"\$\d+");
                        if (priceMatch.Success)
                        {
                            priceStr = priceMatch.Value;
                            Console.WriteLine($"  - Extracted price: {priceStr}");
                        }
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine("  - Precise selector failed. Trying generic fallback search...");
                        try
                        {
                            var fallbackSelector = "//*[contains(text(), '$')]";
                            var potentialElements = driver.FindElements(By.XPath(fallbackSelector));

                            foreach (var element in potentialElements)
                            {
                                var priceMatch = Regex.Match(element.Text, @"\$\d{2,}");
                                if (priceMatch.Success)
                                {
                                    priceStr = priceMatch.Value;
                                    Console.WriteLine($"  - SUCCESS: Found price '{priceStr}' with fallback selector.");
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"  - Fallback search also failed: {e.Message}");
                        }
                    }

                    if (string.IsNullOrEmpty(priceStr))
                    {
                        Console.WriteLine("  - FAILED: Could not find price with any method.");
                    }

                    // F. Insert into database
                    // var hotelId = InsertHotel(connection, name, city, null, null, lat, lon);
                    // Console.WriteLine($"  - Inserted Hotel with ID: {hotelId}");

                    // if (!string.IsNullOrEmpty(priceStr))
                    // {
                    //     var priceMatch = Regex.Match(priceStr, @"[\d\.]+");
                    //     if (priceMatch.Success && decimal.TryParse(priceMatch.Value, out var price))
                    //     {
                    //         InsertPrice(connection, hotelId, price, DateTime.Now.ToString("yyyy-MM-dd"));
                    //         Console.WriteLine($"  - Inserted Price: {price}");
                    //     }
                    // }
                }
            }
            finally
            {
                driver?.Quit();
                httpClient?.Dispose();
            }
        }

        public static async Task AddNewHotelWithPriceAsync(AppDbContext context, string hotelName, decimal priceValue)
        {
            // 1. Create the new HotelPrice object
            var newPrice = new HotelPrice
            {
                Price = priceValue,
                PriceDate = DateTime.UtcNow // Or the specific date for the price
            };

            // 2. Create the new Hotel object
            var newHotel = new Hotel
            {
                Name = hotelName,
                City = "Example City" // Populate other properties as needed
                                      // No need to create the Prices list, it's initialized in the class
            };

            // 3. Link the objects together using the navigation property.
            // This is the magic step where EF Core understands the relationship.
            newHotel.Prices.Add(newPrice);

            // 4. Add the parent object to the DbContext.
            // EF Core is smart enough to know it also needs to insert the child 'newPrice' object.
            context.Hotels.Add(newHotel);

            // 5. Save all changes to the database in a single transaction.
            // EF Core generates the INSERT for Hotels, gets the ID, and then generates
            // the INSERT for HotelPrices with the correct foreign key, all automatically.
            await context.SaveChangesAsync();

            Console.WriteLine($"Successfully inserted hotel '{newHotel.Name}' with ID {newHotel.Id}");
        }
    }
}