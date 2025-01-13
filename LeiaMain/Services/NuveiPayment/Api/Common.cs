namespace Services.NuveiPayment.Api
{
    public class DeviceDetails
    {
        public string ipAddress;
    }

    public class PaymentOptionCard
    {
        public string cardNumber;
        public string cardHolderName;
        public string expirationMonth;
        public string expirationYear;
        public string CVV;
        public object threeD; // Need to research this further 
    }

    public class PaymentOptionRoot
    {
        public PaymentOptionCard card;
    }

}
