using EditSearch.Backend.Models;

public interface IHotelService
{
    Task<IEnumerable<Hotel>> GetAllHotelsAsync();
    Task<Hotel?> GetHotelByIdAsync(int id);
    Task<Hotel> AddHotelAsync(Hotel hotel);
    Task<bool> UpdateHotelAsync(Hotel hotel);
    Task<bool> DeleteHotelAsync(int id);
}
