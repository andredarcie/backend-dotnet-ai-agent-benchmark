using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

/// <summary>
/// Health check endpoint.
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Check the health of the API.
    /// </summary>
    /// <returns>Health status.</returns>
    [HttpGet]
    [ProduceResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new HealthResponse { Status = "healthy" });
    }
}

/// <summary>
/// Health response model.
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// The health status.
    /// </summary>
    public required string Status { get; set; }
}
