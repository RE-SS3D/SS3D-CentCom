using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HeartbeatController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            return "{\"message\":\"Honk!\"}";
        }
    }
}