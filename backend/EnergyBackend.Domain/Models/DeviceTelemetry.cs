using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyBackend.Domain.Models;

public class DeviceTelemetry
{
    public int Id { get; set; }
    public string DeviceIdentifier { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double Voltage { get; set; }
    public double Current { get; set; }
    public double Power { get; set; }
    public double Energy { get; set; }  // cumulative
}
