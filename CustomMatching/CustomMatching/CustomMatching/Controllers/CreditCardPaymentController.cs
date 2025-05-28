using Microsoft.AspNetCore.Mvc;
using Services.CreditCards;
using Services.DTO;

namespace CustomMatching.Controllers
{
    [ApiController]
    [Route("payments")]
    public class CreditCardPaymentController:ControllerBase
    {
        CreditCardService creditCardService = new CreditCardService();

        [HttpPost("charge")]
        public async Task<IActionResult> Charge([FromBody] CreditCardChargeRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await creditCardService.ChargeAsync(req);
            return result.Reply == "000"
              ? Ok(result)
              : result.Needs3DS
                ? Ok(result)
                : BadRequest(new { error = result.ReplyDesc });
        }

        [HttpPost("notification")]
        public IActionResult Notification([FromForm] IFormCollection form)
        {
            // TODO: update your DB with form["Reply"], form["TransID"], etc.
            return Ok();
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus([FromQuery] string transId)
        {
            if (string.IsNullOrEmpty(transId))
                return BadRequest("transId is required");

            var result = await creditCardService.GetStatusByIdAsync(transId);
            return Ok(result);
        }
    }
}
