using Microsoft.AspNetCore.Mvc;

namespace FileUploaderApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HomeController : Controller
    {
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
