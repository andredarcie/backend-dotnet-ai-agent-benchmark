using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Api.Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    public ActionResult<object> Get()
    {
        return Ok(new { status = "healthy" });
    }
}
