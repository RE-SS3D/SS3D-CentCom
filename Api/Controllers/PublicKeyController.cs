using System.Text;
using Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicKeyController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        
        public PublicKeyController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        [HttpGet]
        public ActionResult<string> Get() => _appSettings.PublicKey;
    }
}