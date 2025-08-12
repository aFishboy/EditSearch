import sqlite3
from datetime import date

def setup_database(db_file):
    """Connects to the DB and creates tables if they don't exist."""
    conn = sqlite3.connect(db_file)
    cursor = conn.cursor()
    
    # Create Hotels Table
    cursor.execute('''
    CREATE TABLE IF NOT EXISTS hotels (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        city TEXT,
        state TEXT,
        zip_code TEXT,
        latitude REAL,
        longitude REAL
    )
    ''')
    # Create HotelPrices Table
    cursor.execute('''
    CREATE TABLE IF NOT EXISTS hotel_prices (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        hotel_id INTEGER NOT NULL,
        price REAL NOT NULL,
        price_date TEXT NOT NULL,
        last_updated TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY(hotel_id) REFERENCES hotels(id)
    )
    ''')
    print(f"Database '{db_file}' is ready.")
    return conn, cursor

def insert_hotel(cursor, hotel_data):
    """Inserts a new hotel and returns its new ID."""
    sql = "INSERT INTO hotels (name, city, state, zip_code, latitude, longitude) VALUES (?, ?, ?, ?, ?, ?)"
    cursor.execute(sql, hotel_data)
    return cursor.lastrowid # This gets the ID of the row just inserted

def insert_price(cursor, price_data):
    """Inserts a new price record for a hotel."""
    sql = "INSERT INTO hotel_prices (hotel_id, price, price_date) VALUES (?, ?, ?)"
    cursor.execute(sql, price_data)