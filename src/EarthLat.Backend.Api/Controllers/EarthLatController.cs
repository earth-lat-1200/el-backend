using Microsoft.AspNetCore.Mvc;

namespace EarthLat.Backend.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EarthLatController : ControllerBase
    {
        private readonly ILogger<EarthLatController> _logger;

        public EarthLatController(ILogger<EarthLatController> logger)
        {
            _logger = logger;
        }
    }
}