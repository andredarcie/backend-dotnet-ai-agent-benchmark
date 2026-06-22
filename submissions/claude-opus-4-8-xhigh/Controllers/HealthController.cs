using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() => Ok(new { status = "healthy" });
}
