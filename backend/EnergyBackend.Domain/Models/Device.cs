namespace EnergyBackend.Domain.Models;

public class Device
{
    public int Id { get; set; } //pk
    public string DeviceIdentifier { get; set; } // unique identifier or serial number
    public string Name { get; set; }
    public string Location { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
