using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Services.CreditCards;
using Services.DTO;

namespace CustomMatching.Controllers
{
    [ApiController]
    [Route("payments")]
    public class CreditCardPaymentController:ControllerBase
    {
        CreditCardService creditCardService = new CreditCardService();

        [HttpPost("Charge")]
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

        [HttpPost("Redirect")]
        public IActionResult Redirect([FromBody] CreditCardRedirectRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string redirectUrl = creditCardService.GetHostedRedirectUrl(req);

            return Redirect(redirectUrl);
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
