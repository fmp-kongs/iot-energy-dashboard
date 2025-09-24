using Microsoft.AspNetCore.Mvc;

namespace EnergyBackend.Api.Controller;


[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult GetHealth()
    {
        return Ok("Healthy");
    }
}