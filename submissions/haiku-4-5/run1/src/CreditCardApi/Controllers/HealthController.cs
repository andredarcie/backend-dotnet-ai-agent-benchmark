using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new HealthResponse { Status = "healthy" });
    }
}

public record HealthResponse(string Status = "healthy");
