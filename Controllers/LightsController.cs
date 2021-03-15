using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace PlugHub.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LightsController : ControllerBase
    {
        private readonly ILogger<LightsController> _logger;
        private readonly IPhilipsHuePlugService _plugService;

        public LightsController(ILogger<LightsController> logger, IPhilipsHuePlugService plugService)
        {
            _logger = logger;
            _plugService = plugService;
        }

        [HttpGet("{id}/toggle")]
        public async Task<String> Get()
        {
            await _plugService.Toggle();
            return "toggled";
        }
    }
}
