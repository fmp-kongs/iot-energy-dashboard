using EnergyBackend.Data;
using EnergyBackend.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnergyBackend.Api.Controller;

[Route("api/[controller]")]
[ApiController]
public class DeviceController(ApplicationDbContext applicationDbContext) : ControllerBase
{
    private readonly ApplicationDbContext _db = applicationDbContext;

    [HttpGet]
    public async Task<IActionResult> GetDevices()
    {
        var devices = await _db.Devices.ToListAsync();
        return Ok(devices);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDevice(int id)
    {
        var device = await _db.Devices.FindAsync(id);
        if (device == null)
        {
            return NotFound();
        }
        return Ok(device);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDevice([FromBody] Device device)
    {
        _db.Devices.Add(device);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, device);
    }

}
