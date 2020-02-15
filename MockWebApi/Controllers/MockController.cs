using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MockWebApi.Models;

namespace MockWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MockController : ControllerBase
    {
        private readonly ILogger<MockController> _logger;

        public MockController(ILogger<MockController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public MockResponse Get()
        {
            _logger.LogInformation("Getting mock response.");
            
            return new MockResponse
            {
                Name = "mock",
                Summary = "mock"
            };
        }
    }
}