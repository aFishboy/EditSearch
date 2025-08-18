using System;
using System.Globalization;
using System.Text.RegularExpressions;
using EditSearch.Backend.Entities;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace HotelDataImporter.PriceScraper
{
    partial class PriceScraper
    {
        [GeneratedRegex(@"\$\d+")]
        private static partial Regex MyRegex();
        private const string PrimarySelector = "div[aria-label*='View prices']";
        private const string FallbackSelector = "//*[contains(text(), '$') and string-length(normalize-space(text())) < 20]";
        public static decimal ScrapeHotelPrice(Hotel hotelEntity)
        {
            var options = new ChromeOptions();
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
            options.AddExcludedArgument("enable-automation");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            // options.AddArgument("--headless");

            // 1. Use a 'using' statement for cleaner resource management.
            //    The driver will automatically be quit at the end of this block.
            using (IWebDriver driver = new ChromeDriver(options))
            {
                var query = $"{hotelEntity.Name} {hotelEntity.City} hotel price";
                driver.Navigate().GoToUrl($"https://www.google.com/search?q={Uri.EscapeDataString(query)}");

                // --- 1. The new, clean main logic ---
                // Try the best method first. If it returns null, try the next one.
                string? priceStr = TryPrimaryScrape(driver);

                if (priceStr == null)
                {
                    priceStr = TryFallbackScrape(driver);
                }

                // --- 2. Final parsing logic remains the same ---
                if (!string.IsNullOrEmpty(priceStr))
                {
                    if (decimal.TryParse(priceStr.Replace("$", ""), out decimal price))
                    {
                        return price;
                    }
                }

                Console.WriteLine("  - FAILED: Could not find a price with any method.");
                return 0;
            }
        }

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
            return null; // Return null if not found or if an error occurred
        }

        // --- 4. Helper method for the fallback strategy ---
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
            return null; // Return null if not found
        }
    }
}