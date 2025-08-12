import os
import re
import sqlite3
import time
import requests # For API calls
import undetected_chromedriver as uc
from dotenv import load_dotenv
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.chrome.service import Service
from webdriver_manager.chrome import ChromeDriverManager
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import TimeoutException
from database_manager import setup_database, insert_hotel, insert_price

# --- 1. CONFIGURATION ---
load_dotenv()
# --- This new block builds a reliable path to your file ---
# Get the directory where this script is located
script_dir = os.path.dirname(__file__) 
# Join the script's directory with the filename to create a full path
file_path = os.path.join(script_dir, 'hotelListHtml.txt')
DB_FILE = r'C:\Users\Sam\Coding\EditSearch\backend\editsearch.db'
# -----------------------------------------------------------

# --- 2. SETUP DATABASE by calling your new function ---
# conn, cursor = setup_database(DB_FILE)

# --- 3. SETUP WEB SCRAPER ---
# ... (scraper setup code remains the same) ...
options = uc.ChromeOptions()
# options.add_argument('--headless')
driver = uc.Chrome(options=options)
print("Using undetected-chromedriver.")

# 3. GRAB THE DATA FROM THE FILE
try:
    # Use the full file_path variable instead of just the filename
    with open(file_path, 'r', encoding='utf-8') as f:
        long_string = f.read()
except FileNotFoundError:
    print("Error: The file 'hotelListHtml' was not found in this directory.")
    exit() # Exit the script if the file doesn't exist


# 4. SPLIT THE STRING INTO A LIST
# We use the text between the list items as the separator.
delimiter = "</li><li>"
split_list = long_string.split(delimiter)


# 5. CLEAN UP EACH ITEM IN THE LIST
# The split leaves the first item with a leading '<li>' and the last with a trailing '</li>'.
# We loop through the list to clean these artifacts from each item.
cleaned_hotels = []
for hotel_string in split_list:
    # Using removeprefix and removesuffix is a clean way to handle this
    # It safely removes the tags only if they exist at the ends of the string.
    clean_item = hotel_string.removeprefix('<li>').removesuffix('</li>')
    final_item = clean_item.split(' - ', 1)[0].strip()
    cleaned_hotels.append(final_item)


# 6. PRINT THE RESULT TO VERIFY
search_terms = ["Kyoto"]#["JP", "Japan", "Tokyo", "Kyoto", "Osaka", "CA", "California", "HI", "Waikiki", "Honolulu"]
matching_hotels = [h for h in cleaned_hotels if any(term in h for term in search_terms)]

for hotel in matching_hotels:
    print(hotel)
print(f"Matching length: {len(matching_hotels)}.\n")

# Captures 1: The hotel name, and 2: Everything inside the parentheses
pattern = re.compile(r"(.+?)\s+\((.+)\)")

for hotel_info_string in matching_hotels:
    match = pattern.match(hotel_info_string)
    if not match:
        print(f"WARNING: Could not perform initial parse on: {hotel_info_string}")
        continue

    # A. Parse the main parts
    name = match.group(1).strip()
    location_string = match.group(2).strip() # e.g., "Toronto, ON, CA, M4W 0A4"
    if ',' in name:
    # rsplit(',', 1) splits from the right, only once.
    # [0] takes the first part (the actual name).
        name = name.rsplit(',', 1)[0].strip()

    # B. Split the location string to analyze its components
    location_parts = [part.strip() for part in location_string.split(',')]
    
    # C. Extract what you can (this logic can be improved as needed)
    city = location_parts[0] if len(location_parts) > 0 else 'N/A'
    country = 'N/A'
    for part in location_parts:
        if part in ['US', 'CA', 'MX', 'FR', 'JP', 'IE']: # Add more known country codes
            country = part
            break
    
    print(f"Successfully Parsed: {name} | Location: {location_string} | Country: {country}")

    lat, lon = None, None
    try:
        # Combine the name and the full location string for the best results
        search_text = f"{name}, {location_string}"
        
        # Construct the URL. The `requests` library will automatically handle encoding
        # special characters like spaces and commas in your search_text.
        geo_url = f"https://api.geoapify.com/v1/geocode/search?text={search_text}&apiKey={os.getenv('GEOAPIFY_API_KEY')}"
        
        print(f"  - Geocoding query: {search_text}") # Good for debugging
        
        # geo_response = requests.get(geo_url)
        # geo_data = geo_response.json()
        
        # if geo_data['features']:
        #     lon, lat = geo_data['features'][0]['geometry']['coordinates']
        #     print(f"  - Geocoded Coordinates: {lat}, {lon}")

    except Exception as e:
        print(f"  - Geocoding failed: {e}")

    price_str = None # Initialize with a default value
    try:
        query = f"{name} {city} price"
        print(f"https://www.google.com/search?q={query}")
        driver.get(f"https://www.google.com/search?q={query}")

        # Wait up to 10 seconds for the element to be present
        wait = WebDriverWait(driver, 10)
        # Use a more precise CSS selector to find the element
        selector = "div[aria-label*='View prices']"
        
        # Correctly use the wait object to find the element
        price_element = wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, selector)))

        # Get the full text from the aria-label attribute
        full_label = price_element.get_attribute('aria-label')
        print(f"  - Found label: '{full_label}'")

        # Use a regular expression to find the dollar amount
        price_match = re.search(r'\$\d+', full_label)
        if price_match:
            price_str = price_match.group(0) # Extracts the matched text, e.g., "$815"
            print(f"  - Extracted price: {price_str}")

    except TimeoutException:
        # --- ATTEMPT 2: The Fallback Search ---
        print("  - Precise selector failed. Trying generic fallback search...")
        try:
            # Find any element (*) on the page whose text contains a '$'
            fallback_selector = "//*[contains(text(), '$')]"
            
            # Use find_elements (plural) as this may find multiple results
            potential_elements = driver.find_elements(By.XPATH, fallback_selector)
            
            for element in potential_elements:
                # Check if the element's text truly matches a price pattern
                price_match = re.search(r'\$\d{2,}', element.text) # Look for at least 2 digits
                if price_match:
                    price_str = price_match.group(0)
                    print(f"  - SUCCESS: Found price '{price_str}' with fallback selector.")
                    break # Stop after finding the first valid-looking price
        except Exception as e:
            print(f"  - Fallback search also failed: {e}")

    # After the try/except block, price_str will either have a value or be None
    if not price_str:
        print("  - FAILED: Could not find price with any method.")
