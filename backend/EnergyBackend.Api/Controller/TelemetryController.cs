using EnergyBackend.Domain.Models;
using EnergyBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnergyBackend.Api.Controller;

[Route("api/[controller]")]
[ApiController]
public class TelemetryController(InfluxDbService influxDbService) : ControllerBase
{
    private readonly InfluxDbService _influxDbService = influxDbService;

    [HttpPost]
    public async Task<IActionResult> PostTelemetry([FromBody] DeviceTelemetry deviceTelemetry)
    {
        await _influxDbService.WriteTelemetryAsync(
            deviceTelemetry.DeviceIdentifier,
            deviceTelemetry.Timestamp,
            deviceTelemetry.Voltage,
            deviceTelemetry.Current,
            deviceTelemetry.Power
            );
        return Ok();
    }

    [HttpGet("{deviceId}")]
    public async Task<IActionResult> GetTelemetry(string deviceId)
    {
        return Ok();
    }

    [HttpPost("send-test")]
    public async Task<IActionResult> SendTestTelemetry()
    {
        var testTelemetry = new DeviceTelemetry
        {
            DeviceIdentifier = "test-device-001",
            Timestamp = DateTime.UtcNow,
            Voltage = 230.0,
            Current = 5.0,
            Power = 1150.0,
            Energy = 0.0
        };
        await _influxDbService.WriteTelemetryAsync(
            testTelemetry.DeviceIdentifier,
            testTelemetry.Timestamp,
            testTelemetry.Voltage,
            testTelemetry.Current,
            testTelemetry.Power
        );
        return Ok("Test telemetry sent.");
    }
}
