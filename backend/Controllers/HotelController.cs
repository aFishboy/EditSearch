using Microsoft.AspNetCore.Mvc;
using EditSearch.Backend.Models;

namespace EditSearch.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelController : ControllerBase
{
    private readonly IHotelService _hotelService;

    public HotelController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Hotel>>> GetHotels() =>
        Ok(await _hotelService.GetAllHotelsAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Hotel>> GetHotel(int id)
    {
        var hotel = await _hotelService.GetHotelByIdAsync(id);
        if (hotel == null) return NotFound();
        return hotel;
    }

    [HttpPost]
    public async Task<ActionResult<Hotel>> PostHotel(Hotel hotel)
    {
        var createdHotel = await _hotelService.AddHotelAsync(hotel);
        return CreatedAtAction(nameof(GetHotel), new { id = createdHotel.Id }, createdHotel);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutHotel(int id, Hotel hotel)
    {
        if (id != hotel.Id) return BadRequest();

        var updated = await _hotelService.UpdateHotelAsync(hotel);
        if (!updated) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHotel(int id)
    {
        var deleted = await _hotelService.DeleteHotelAsync(id);
        if (!deleted) return NotFound();

        return NoContent();
    }
}
