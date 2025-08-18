namespace HotelDataImporter.ParseHotelList
{
    class ParseHotelList
    {
        public static async Task<List<string>> GetHotelListFromFile()
        {
            // Build reliable path to file
            var scriptDir = Directory.GetCurrentDirectory();
            var filePath = Path.Combine(scriptDir, "hotelListHtml.txt");

            // 4. GRAB DATA FROM FILE
            string longString;
            try
            {
                longString = await File.ReadAllTextAsync(filePath);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Error: The file 'hotelListHtml.txt' was not found in this directory.");
                throw new Exception("Can't find file");
            }

            // 5. SPLIT THE STRING INTO A LIST
            var delimiter = "</li>";
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

                var finalItem = cleanItem.Split(new[] { ") - " }, 2, StringSplitOptions.None)[0].Trim() + ")";
                cleanedHotels.Add(finalItem);
            }

            // 7. FILTER BY SEARCH TERMS
            // var searchTerms = new[] { "Japan", "JP", "Tokyo", "Kyoto", "Osaka" };
            var searchTerms = new[] { "" };
            var matchingHotels = cleanedHotels.Where(h => searchTerms.Any(term => h.Contains(term))).ToList();

            foreach (var hotel in matchingHotels)
            {
                Console.WriteLine(hotel);
            }
            Console.WriteLine($"Matching length: {matchingHotels.Count}.\n");

            return matchingHotels;
        }
    }
}