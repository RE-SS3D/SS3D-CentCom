using Microsoft.AspNetCore.Mvc;

namespace CentCom.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HeartbeatController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            return "Honk!";
        }
    }
}