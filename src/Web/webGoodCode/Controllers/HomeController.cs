using Microsoft.AspNetCore.Mvc;

namespace webGoodCode.Controllers
{
    [Route("/")]
    public class HomeController : ControllerBase
    {
        public IActionResult Get()
        {
            return Ok("Hellow World");
        }
    }
}
