using Microsoft.AspNetCore.Mvc;
using Services.DTO;
using Services;
using DataObjects;

namespace CustomMatching.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ZealyController : ControllerBase
    {
        private readonly ISuikaDbService _suikaDbService;

        public ZealyController(ISuikaDbService suikaDbService)
        {
            _suikaDbService = suikaDbService;
        }

        /// <summary>
        /// POST /Zealy/RecordXp
        /// Body: { playerId, playerName, taskName, taskDescription, xpAmount }
        /// Inserts a new row into ZealyData.
        /// </summary>
        [HttpPost, Route("RecordXp")]
        public async Task<IActionResult> RecordXp([FromBody] ZealyDataRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var entry = new ZealyData
                {
                    PlayerId = request.PlayerId,
                    PlayerName = request.PlayerName,
                    TaskName = request.TaskName,
                    TaskDescription = request.TaskDescription,
                    XpAmount = request.XpAmount,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _suikaDbService.LeiaContext.ZealyData.Add(entry);
                await _suikaDbService.LeiaContext.SaveChangesAsync();

                // Return 200 OK (or 201 Created if you prefer)
                return Ok(new { success = true, id = entry.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while saving data.");
            }
        }
    }
}
