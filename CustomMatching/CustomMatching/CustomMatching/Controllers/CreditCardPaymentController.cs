using Microsoft.AspNetCore.Mvc;
using Services.Credit_cards;
using Services.DTO;

namespace CustomMatching.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class CreditCardPaymentController:ControllerBase
    {
        [HttpPost("charge")]
        public async Task<IActionResult> Charge([FromBody] CreditCardChargeRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            Credit_Card_Service creditCardService = new Credit_Card_Service();

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
    }
}
