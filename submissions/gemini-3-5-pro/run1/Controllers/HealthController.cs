using Microsoft.AspNetCore.Mvc;

namespace CreditCardApi.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new { status = "healthy" });
        }
    }
}
