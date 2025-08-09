using EditSearch.Backend.Data;    // For AppDbContext
using EditSearch.Backend.Models;  // For Hotel model
using Microsoft.EntityFrameworkCore; // For EF Core async extensions
using System.Collections.Generic; // For IEnumerable<>
using System.Threading.Tasks;     // For Task<>
using System.Linq;
public class HotelService : IHotelService
{
    private readonly AppDbContext _context;

    public HotelService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Hotel>> GetAllHotelsAsync() =>
        await _context.Hotels.ToListAsync();

    public async Task<Hotel?> GetHotelByIdAsync(int id) =>
        await _context.Hotels.FindAsync(id);

    public async Task<Hotel> AddHotelAsync(Hotel hotel)
    {
        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();
        return hotel;
    }

    public async Task<bool> UpdateHotelAsync(Hotel hotel)
    {
        if (!_context.Hotels.Any(h => h.Id == hotel.Id))
            return false;

        _context.Entry(hotel).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteHotelAsync(int id)
    {
        var hotel = await _context.Hotels.FindAsync(id);
        if (hotel == null) return false;

        _context.Hotels.Remove(hotel);
        await _context.SaveChangesAsync();
        return true;
    }
}
