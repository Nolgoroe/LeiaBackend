using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CustomMatching.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ConfigurationDataController : ControllerBase
    {
        private readonly ISuikaDbService _suikaDbService;

        public ConfigurationDataController(ISuikaDbService suikaDbService)
        {
            _suikaDbService = suikaDbService;
        }

        // GET /GetConfigurationByBuildNumber/1
        [HttpGet, Route("GetConfigurationByBuildNumber/{buildNumber}")]
        public async Task<IActionResult>  GetConfigurationByBuildNumber( string buildNumber)
        {
            var configurations = await _suikaDbService.LeiaContext.ConfigurationsData.FirstOrDefaultAsync( c => c.AppVersion == buildNumber);
            if (configurations == null)return NotFound("Configuration not found");
            
            return Ok(configurations);
        }

        // GET api/<ConfigurationDataController>/5
        [HttpGet("{id}")]
        private string Get(int id)
        {
            return "value";
        }

        // POST api/<ConfigurationDataController>
        [HttpPost]
        private void Post([FromBody] string value)
        {
        }

        // PUT api/<ConfigurationDataController>/5
        [HttpPut("{id}")]
        private void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ConfigurationDataController>/5
        [HttpDelete("{id}")]
        private void Delete(int id)
        {
        }
    }
}
