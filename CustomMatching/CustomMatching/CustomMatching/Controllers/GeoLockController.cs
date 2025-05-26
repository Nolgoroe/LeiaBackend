using DAL;
using DataObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services;

namespace CustomMatching.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GeoLockController : ControllerBase
    {
        private readonly ISuikaDbService _suikaDbService;

        public GeoLockController(ISuikaDbService suikaDbService)
        {
            _suikaDbService = suikaDbService;
        }

        /// <summary>
        /// Returns all configured 2-letter country codes.
        /// </summary>
        [HttpGet("countries")]
        public async Task<IActionResult> GetAllCountryCodes()
        {
            var codes = await _suikaDbService.LeiaContext
                                            .GeoLockLocations
                                            .AsNoTracking()
                                            .Select(gl => gl.CountryCode)
                                            .ToArrayAsync();

            return Ok(codes);
        }
    }
}
