using Microsoft.EntityFrameworkCore;
using EditSearch.Backend.Entities;

namespace EditSearch.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<HotelPrice> HotelPrice { get; set; }
}
