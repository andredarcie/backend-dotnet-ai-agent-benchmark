using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Api.Presentation.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    [Produces("application/json")]
    public ActionResult<HealthResponse> GetHealth()
    {
        return Ok(new HealthResponse { Status = "healthy" });
    }
}

public class HealthResponse
{
    public string Status { get; set; } = null!;
}
