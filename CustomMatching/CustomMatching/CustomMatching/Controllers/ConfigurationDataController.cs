using Microsoft.AspNetCore.Mvc;

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

        // GET /GetConfigurationById/1
        [HttpGet, Route("GetConfigurationById/{Id}")]
        public async Task<IActionResult>  GetConfigurationById( int Id)
        {
            var configurations = await _suikaDbService.LeiaContext.ConfigurationsData.FindAsync(Id);
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
