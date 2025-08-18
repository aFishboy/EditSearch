using System.Text.Json;
using System.Text.RegularExpressions;
using EditSearch.Backend.Data;
using EditSearch.Backend.Entities;
using Microsoft.Data.Sqlite;

namespace HotelDataImporter.GeoLocateHotel
{
    class GeoLocateHotel
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public static async Task GeoLocateHotelList(string geoapifyApiKey, string DB_FILE, AppDbContext context, List<string> hotelList)
        {
            string logPath = @"C:\Users\Sam\Coding\EditSearch\logs";

            // 8. PROCESS EACH HOTEL
            var pattern = new Regex(@"(.+?)\s+\((.+)\)");

            using var connection = new SqliteConnection($"Data Source={DB_FILE}");
            connection.Open();

            var existingPlaceIds = new HashSet<string>(context.Hotels.Select(h => h.PlaceID));
            int count = 1;
            int skipCount = 0;
            int failedFeaturesCount = 0;
            int failedToProcessCount = 0;
            int failedToParseCount = 0;
            foreach (var hotelInfoString in hotelList)
            {
                // if (!hotelInfoString.Contains("The Ritz-Carlton, Tokyo", StringComparison.OrdinalIgnoreCase))
                // {
                //     continue;
                // }
                Console.WriteLine("\n----------------Hotel Count " + count++
                    + " | Skip count " + skipCount + " | No Feature count "
                    + failedFeaturesCount + " | Failed to process " + failedToProcessCount
                    + " | Failed to parse " + failedToParseCount + "--------------------");

                var newHotel = new Hotel { };
                var match = pattern.Match(hotelInfoString);
                if (!match.Success)
                {
                    failedToParseCount++;
                    Console.WriteLine($"WARNING: Could not perform initial parse on: {hotelInfoString}");
                    string logFilePath = "parse_fail.log";
                    string fullPath = Path.Combine(logPath, logFilePath);
                    string errorMessage = $"---------Failed to parse. {hotelInfoString} ------------\n";
                    File.AppendAllText(fullPath, errorMessage);
                    continue;
                }

                // A. Parse the main parts
                var parsedName = match.Groups[1].Value.Trim();
                var locationString = match.Groups[2].Value.Trim();

                // if (parsedName.Contains(','))
                // {
                //     var lastCommaIndex = parsedName.LastIndexOf(',');
                //     parsedName = parsedName.Substring(0, lastCommaIndex).Trim();
                // }

                // B. Split location string
                // B. Split location string
                var locationParts = locationString.Split(',')
                                                  .Select(p => p.Trim())
                                                  .ToArray();

                string parsedPostal = "";
                string parsedCountry = "";
                string parsedState = "";
                string parsedCity = "";

                // C. Extract from the end
                if (locationParts.Length >= 2)
                {
                    parsedPostal = locationParts[^1];   // last element
                    parsedCountry = locationParts[^2];  // second to last
                }
                if (locationParts.Length >= 3)
                {
                    parsedState = locationParts[^3];    // optional
                }
                if (locationParts.Length >= 1)
                {
                    parsedCity = locationParts[0];      // always first
                }

                string countryQuery = "";
                if (!string.IsNullOrEmpty(parsedCountry))
                {
                    countryQuery = $"&filter=countrycode:{parsedCountry.ToLowerInvariant()}";
                }

                Console.WriteLine($"Successfully Parsed: {parsedName} | Location: {locationString} | Country: {parsedCountry}");

                // D. Geocoding
                try
                {
                    var searchText = $"{parsedName}, {locationString}";
                    // parsedName = parsedName.Trim('\'', '"', ' ');
                    var geoUrl = $"https://api.geoapify.com/v1/geocode/search?text={Uri.EscapeDataString(parsedName)}{countryQuery}&type=amenity&apiKey={geoapifyApiKey}";
                    string allGeoUrlsFile = "allGeoUrls.log";
                    string geoUrlPath = Path.Combine(logPath, allGeoUrlsFile);
                    File.AppendAllText(geoUrlPath, geoUrl + "\n");

                    if (context.Hotels.Any(h => h.Name.Equals(parsedName)))
                    {
                        string duplicateMessage = $"   Duplicate parsed {parsedName}\n {geoUrl} \n skipping...";
                        Console.WriteLine(duplicateMessage);
                        skipCount++;
                        string logFilePath = "duplicateParse.log";

                        string path = Path.Combine(logPath, logFilePath);
                        File.AppendAllText(path, duplicateMessage);
                        continue;
                    }

                    Console.WriteLine($"  - Geocoding query: {geoUrl}");

                    try
                    {
                        var geoResponse = await httpClient.GetStringAsync(geoUrl);
                        var geoData = JsonDocument.Parse(geoResponse);
                        var features = geoData.RootElement.GetProperty("features");

                        if (features.GetArrayLength() > 0)
                        {
                            Console.WriteLine("     Has features...");
                            // Inside your `if (features.GetArrayLength() > 0)` block:

                            var properties = features[0].GetProperty("properties");

                            string placeID = properties.TryGetProperty("place_id", out var place_id) ? place_id.GetString() ?? string.Empty : string.Empty;
                            if (existingPlaceIds.Contains(placeID))
                            {
                                newHotel.Name = parsedName;
                                newHotel.CountryCode = parsedCountry.ToLower();
                                context.Hotels.Add(newHotel);
                                Console.WriteLine($"Skipping duplicate placeID: {hotelInfoString}");
                                continue;
                            }

                            // --- CORRECTED CODE ---
                            // Set the properties of the existing hotelDto object one by one
                            newHotel.Name = parsedName;
                            newHotel.FoundApiName = properties.TryGetProperty("name", out var nameElem)
                                ? nameElem.GetString() ?? parsedName
                                : parsedName;
                            newHotel.PlaceID = placeID;
                            newHotel.City = properties.TryGetProperty("city", out var cityElem) ? cityElem.GetString() : null;
                            newHotel.State = properties.TryGetProperty("state", out var stateElem) ? stateElem.GetString() : null;
                            newHotel.County = properties.TryGetProperty("county", out var countyElem) ? countyElem.GetString() : null;
                            newHotel.Postcode = properties.TryGetProperty("postcode", out var postcodeElem) ? postcodeElem.GetString() : null;
                            newHotel.Country = properties.TryGetProperty("country", out var countryElem) ? countryElem.GetString() : null;
                            newHotel.CountryCode = properties.TryGetProperty("country_code", out var countryCodeElem) ? countryCodeElem.GetString() : null;
                            newHotel.FormattedAddress = properties.TryGetProperty("formatted", out var formattedElem) ? formattedElem.GetString() : null;
                            newHotel.Latitude = properties.TryGetProperty("lat", out var latElem) ? latElem.GetDouble() : null;
                            newHotel.Longitude = properties.TryGetProperty("lon", out var lonElem) ? lonElem.GetDouble() : null;

                            string category = properties.TryGetProperty("category", out var tempCat) ? tempCat.GetString() ?? string.Empty : string.Empty;

                            if (string.IsNullOrEmpty(newHotel.Name) || string.IsNullOrEmpty(newHotel.PlaceID))
                            {
                                Console.WriteLine("     Failed to process hotel...");

                                // Collect reasons
                                List<string> reasons = new();
                                if (string.IsNullOrEmpty(newHotel.Name))
                                    reasons.Add("Missing Name");
                                if (string.IsNullOrEmpty(newHotel.PlaceID))
                                    reasons.Add("Missing PlaceID");

                                failedToProcessCount++;
                                string logFilePath = "failed_to_process.log";

                                // Build log message
                                string errorMessage =
                                    $"---------Failed to process hotel. Hotel parsed name: {parsedName} | Category: {category} ------------\n" +
                                    $"Reasons: {string.Join(", ", reasons)}\n" +
                                    $"{geoUrl}\n";

                                string path = Path.Combine(logPath, logFilePath);
                                File.AppendAllText(path, errorMessage);
                                continue;
                            }

                            string successFilePath = "success.log";
                            string message = $"------------- Category: {category} | GeoResponse: {geoResponse} ------------------\n";
                            string fullPath = Path.Combine(logPath, successFilePath);
                            File.AppendAllText(fullPath, message);


                            existingPlaceIds.Add(newHotel.PlaceID);
                            context.Hotels.Add(newHotel);
                            Console.WriteLine($"  - Inserted '{newHotel.Name}'");
                        }
                        else
                        {
                            Console.WriteLine("No Features Found!!!");
                            failedFeaturesCount++;
                            string logFilePath = "no_features.log";
                            string errorMessage = $"---------Failed to find features. Hotel parsed name: {parsedName} ------------\n";
                            errorMessage += $"{hotelInfoString}\n{geoUrl}\n";
                            string fullPath = Path.Combine(logPath, logFilePath);
                            File.AppendAllText(fullPath, errorMessage);

                            newHotel.Name = parsedName;
                            newHotel.CountryCode = parsedCountry.ToLower();
                            context.Hotels.Add(newHotel);
                            continue;
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
    }
}