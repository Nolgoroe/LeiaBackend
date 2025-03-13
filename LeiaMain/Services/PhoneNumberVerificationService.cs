using Twilio;
using Twilio.Rest.Verify.V2.Service;

namespace Services.PhoneNumberVerification
{

    public interface IPhoneNumberVerificationService
    {
        Task SendVerificationCode(string phoneNumber);
        Task<bool> VerifyReceivedCode(string phoneNumber, string code);
    }

    public class PhoneNumberVerificationService : IPhoneNumberVerificationService
    {

        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _serviceSid;

        public PhoneNumberVerificationService(string accountSid, string authToken, string serviceSid)
        {
            _accountSid = accountSid;
            _authToken = authToken;
            _serviceSid = serviceSid;

            TwilioClient.Init(_accountSid, _authToken);
        }

        public async Task SendVerificationCode(string phoneNumber)
        {
            await VerificationResource.CreateAsync(
                to: phoneNumber,
                channel: "sms",
                pathServiceSid: _serviceSid
            );
        }

        public async Task<bool> VerifyReceivedCode(string phoneNumber, string code)
        {
            VerificationCheckResource response = await VerificationCheckResource.CreateAsync(
                to: phoneNumber,
                code: code,
                pathServiceSid: _serviceSid
            );
            return response.Status.ToLower() == "approved";
        }
    }
}
