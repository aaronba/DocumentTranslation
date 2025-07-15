using Microsoft.AspNetCore.Mvc;

namespace DocumentTranslation.Web.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConfigController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("auth")]
        public IActionResult GetAuthConfig()
        {
            var config = new
            {
                clientId = _configuration["AzureAd:ClientId"],
                tenantId = _configuration["AzureAd:TenantId"],
                authority = _configuration["AzureAd:Instance"],
                isConfigured = !string.IsNullOrEmpty(_configuration["AzureAd:ClientId"])
            };

            return Ok(config);
        }
    }
}