## Integration with Nuvei for credit-card processing

### Required env variables

-   `NUVEI_API_BASE_URL` (protocol://host/path)
-   `NUVEI_MERCHANT_ID` (number)
-   `NUVEI_MERCHANT_SITE_ID` (number)
-   `NUVEI_SECRET_KEY` (string)

### Tokenization of card details

Nuvei allows for [storing debit/credit card information on their system](https://docs.nuvei.com/documentation/features/card-operations/card-on-file/) for future transactions.  
After making a payment, Nuvei's API returns an ID (`userPaymentOptionId`) which, along with the user's ID in Leia's system (`userTokenId`), can be used in future payments instead of the card's information.

### Init Payment

Nuvei requires an `initPayment` request to be performed before the `payment` call.  
As part of the `initPayment` call, Nuvei [initializes the payment in the system](http://docs.nuvei.com/api/main/indexMain_v1_0.html?json#initPayment).  
Some reasons for a payment decline can be found in the `initPayment` response, before the user is notified about an attempted payment.

### Adding currencies

Nuvei expects [3-letter ISO 4217 currency codes](https://docs.nuvei.com/documentation/additional-links/country-and-currency-codes/#currency-codes).  
Adding a new currency requires coordination with Nuvei's representatives.  
There are 2 options to process a new currency:

1. Using the same merchant ID and sending a different currency code via the API. This usually means that the funds will be converted to the account's base currency.
2. Using a different merchant ID for each currency or some of the currencies. Each merchant ID is associated with a base currency and sometimes a different bank account.

### Cash-out

1. **Refunds**; returning money to the user against a previous payment.  
   Refunds can be partial, up to the original payment's amount.  
   For example, a user paid $10. We can refund $5 and later refund another $5. Each refund will get a separate ID.
2. **Payouts**; transferring money to the user's debit/credit card without referencing a payment from the user to the merchant (Leia).

### Webhooks (DMNs)

Nuvei refers to webhooks as [Direct Merchant Notifications (DMNs)](https://docs.nuvei.com/documentation/integration/webhooks/).  
Most payment-related actions take time to settle (minutes to days) and in order to know when they completed, Nuvei provides webhook calls.  
Some advanced flows (like 3DSecure payments) require the use of webhook as part of the flow.  
There's also the option to use an API call "[getTransactionDetails](http://docs.nuvei.com/api/main/indexMain_v1_0.html?json#getTransactionDetails)" to pull information about a recent transaction.
