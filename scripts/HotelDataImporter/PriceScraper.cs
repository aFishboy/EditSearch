using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using EditSearch.Backend.Entities;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace HotelDataImporter.PriceScraper
{
    public enum PriceLevel
    {
        Low,
        Medium,
        High
    }

    public class MonthlyPriceData
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public PriceLevel Level { get; set; }
    }

    partial class PriceScraper
    {
        [GeneratedRegex(@"\$\d+")]
        private static partial Regex MyRegex();

        private const string PrimarySelector = "div[aria-label*='View prices']";
        private const string FallbackSelector = "//*[contains(text(), '$') and string-length(normalize-space(text())) < 20]";

        // Hotel booking site selectors for more comprehensive price data
        private const string BookingComPriceSelector = "[data-testid='price-and-discounted-price']";
        private const string ExpediaPriceSelector = ".offer-price";

        public static List<MonthlyPriceData> ScrapeMonthlyHotelPrice(Hotel hotelEntity)
        {
            var monthlyPrices = new List<MonthlyPriceData>();
            var options = CreateChromeOptions();

            using (IWebDriver driver = new ChromeDriver(options))
            {
                // Try different strategies to get monthly pricing

                // Strategy 1: Try hotel booking sites with date ranges
                var bookingSitePrices = TryBookingSites(driver, hotelEntity);
                if (bookingSitePrices.Any())
                {
                    monthlyPrices.AddRange(bookingSitePrices);
                }
                else
                {
                    // Strategy 2: Fallback to Google searches with date hints
                    var googlePrices = TryGoogleWithDateHints(driver, hotelEntity);
                    monthlyPrices.AddRange(googlePrices);
                }
            }

            // Categorize prices into Low/Medium/High
            if (monthlyPrices.Any())
            {
                CategorizePrices(monthlyPrices);
            }

            return monthlyPrices;
        }

        private static ChromeOptions CreateChromeOptions()
        {
            var options = new ChromeOptions();
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
            options.AddExcludedArgument("enable-automation");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--disable-web-security");
            options.AddArgument("--disable-features=VizDisplayCompositor");
            // Remove headless for debugging, add back for production
            // options.AddArgument("--headless");
            return options;
        }

        private static List<MonthlyPriceData> TryBookingSites(IWebDriver driver, Hotel hotelEntity)
        {
            var prices = new List<MonthlyPriceData>();

            // Try Booking.com first
            prices.AddRange(TryBookingCom(driver, hotelEntity));

            if (!prices.Any())
            {
                // Try Expedia as fallback
                prices.AddRange(TryExpedia(driver, hotelEntity));
            }

            return prices;
        }

        private static List<MonthlyPriceData> TryBookingCom(IWebDriver driver, Hotel hotelEntity)
        {
            var prices = new List<MonthlyPriceData>();

            try
            {
                // Search for the hotel on Booking.com
                var searchUrl = $"https://www.booking.com/searchresults.html?ss={Uri.EscapeDataString($"{hotelEntity.Name} {hotelEntity.City}")}";
                driver.Navigate().GoToUrl(searchUrl);

                Thread.Sleep(3000); // Wait for page load

                // Look for calendar or date picker to get different month prices
                var calendarButton = driver.FindElements(By.CssSelector("[data-testid='date-display-field-start']")).FirstOrDefault();

                if (calendarButton != null)
                {
                    calendarButton.Click();
                    Thread.Sleep(2000);

                    // Try to get prices for different months
                    for (int month = 1; month <= 12; month++)
                    {
                        var monthPrice = TryGetPriceForMonth(driver, month);
                        if (monthPrice > 0)
                        {
                            prices.Add(new MonthlyPriceData
                            {
                                Month = month,
                                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                                Price = monthPrice
                            });
                        }
                    }
                }

                Console.WriteLine($"Booking.com found {prices.Count} monthly prices");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Booking.com scraping failed: {ex.Message}");
            }

            return prices;
        }

        private static List<MonthlyPriceData> TryExpedia(IWebDriver driver, Hotel hotelEntity)
        {
            var prices = new List<MonthlyPriceData>();

            try
            {
                var searchUrl = $"https://www.expedia.com/Hotels-Search?destination={Uri.EscapeDataString($"{hotelEntity.Name} {hotelEntity.City}")}";
                driver.Navigate().GoToUrl(searchUrl);

                Thread.Sleep(5000); // Expedia is slower to load

                // Similar logic to Booking.com but with Expedia selectors
                // Implementation would be similar but with different CSS selectors

                Console.WriteLine("Expedia scraping attempted (implementation needed)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Expedia scraping failed: {ex.Message}");
            }

            return prices;
        }

        private static decimal TryGetPriceForMonth(IWebDriver driver, int month)
        {
            try
            {
                // This is a simplified example - actual implementation would need
                // to navigate calendar and select specific dates
                var nextYear = DateTime.Now.Year + 1;
                var targetDate = new DateTime(nextYear, month, 15); // Mid-month

                // Look for calendar day elements and click on target date
                var dayElement = driver.FindElements(By.CssSelector($"[data-date*='{targetDate:yyyy-MM-dd}']")).FirstOrDefault();

                if (dayElement != null)
                {
                    dayElement.Click();
                    Thread.Sleep(2000);

                    // Look for price after date selection
                    var priceElements = driver.FindElements(By.CssSelector(BookingComPriceSelector));
                    foreach (var element in priceElements)
                    {
                        var priceText = element.Text;
                        var match = MyRegex().Match(priceText);
                        if (match.Success && decimal.TryParse(match.Value.Replace("$", ""), out decimal price))
                        {
                            return price;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting price for month {month}: {ex.Message}");
            }

            return 0;
        }

        private static List<MonthlyPriceData> TryGoogleWithDateHints(IWebDriver driver, Hotel hotelEntity)
        {
            var prices = new List<MonthlyPriceData>();
            var seasons = new[]
            {
                (months: new[] { 12, 1, 2 }, name: "winter", season: "Winter"),
                (months: new[] { 3, 4, 5 }, name: "spring", season: "Spring"),
                (months: new[] { 6, 7, 8 }, name: "summer", season: "Summer"),
                (months: new[] { 9, 10, 11 }, name: "fall", season: "Fall")
            };

            foreach (var season in seasons)
            {
                try
                {
                    var query = $"{hotelEntity.Name} {hotelEntity.City} hotel price {season.name} {DateTime.Now.Year + 1}";
                    driver.Navigate().GoToUrl($"https://www.google.com/search?q={Uri.EscapeDataString(query)}");

                    Thread.Sleep(3000);

                    var priceStr = TryPrimaryScrape(driver) ?? TryFallbackScrape(driver);

                    if (!string.IsNullOrEmpty(priceStr) && decimal.TryParse(priceStr.Replace("$", ""), out decimal price))
                    {
                        // Add price for each month in the season
                        foreach (var month in season.months)
                        {
                            prices.Add(new MonthlyPriceData
                            {
                                Month = month,
                                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                                Price = price
                            });
                        }
                    }

                    Console.WriteLine($"Google search for {season.season}: {priceStr ?? "No price found"}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scraping {season.season} prices: {ex.Message}");
                }
            }

            return prices;
        }

        private static void CategorizePrices(List<MonthlyPriceData> prices)
        {
            if (!prices.Any()) return;

            var minPrice = prices.Min(p => p.Price);
            var maxPrice = prices.Max(p => p.Price);
            var priceRange = maxPrice - minPrice;

            // Define thresholds
            var lowThreshold = minPrice + (priceRange * 0.33m);
            var highThreshold = minPrice + (priceRange * 0.67m);

            foreach (var price in prices)
            {
                if (price.Price <= lowThreshold)
                    price.Level = PriceLevel.Low;
                else if (price.Price >= highThreshold)
                    price.Level = PriceLevel.High;
                else
                    price.Level = PriceLevel.Medium;
            }
        }

        // Your existing methods remain the same
        private static string? TryPrimaryScrape(IWebDriver driver)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                var priceElement = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(PrimarySelector)));
                var fullLabel = priceElement.GetAttribute("aria-label");

                if (!string.IsNullOrEmpty(fullLabel))
                {
                    var priceMatch = MyRegex().Match(fullLabel);
                    if (priceMatch.Success)
                    {
                        Console.WriteLine($"  - SUCCESS: Found price '{priceMatch.Value}' with primary selector.");
                        return priceMatch.Value;
                    }
                }
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("  - INFO: Primary selector timed out, as expected sometimes.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  - ERROR: An unexpected error occurred during primary scrape: {ex.Message}");
            }
            return null;
        }

        private static string? TryFallbackScrape(IWebDriver driver)
        {
            Console.WriteLine("  - INFO: Trying generic fallback search...");
            try
            {
                var potentialElements = driver.FindElements(By.XPath(FallbackSelector));
                foreach (var element in potentialElements)
                {
                    var text = element.Text;
                    var priceMatch = MyRegex().Match(text);
                    if (priceMatch.Success)
                    {
                        Console.WriteLine($"  - SUCCESS: Found price '{priceMatch.Value}' with fallback selector.");
                        return priceMatch.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  - ERROR: Fallback search failed: {ex.Message}");
            }
            return null;
        }
    }
}