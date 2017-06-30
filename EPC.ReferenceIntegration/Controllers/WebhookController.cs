using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using EPC.ReferenceIntegration.Models;
using EPC.ReferenceIntegration.ExtensionMethods;
using EPC.ReferenceIntegration.Helpers;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using EPC.ReferenceIntegration.Wrappers;
using EPC.ReferenceIntegration.DataStore;

namespace EPC.ReferenceIntegration.Controllers
{
    //[Produces("application/json")]
    [Route("api/Webhook")]
    public class WebHookController : Controller
    {
        private readonly AppSettings _AppSettings;
        private ILoggerFactory _Factory;
        private ILogger _Logger;
        private string _ClassName = string.Empty;

        public WebHookController(IOptions<AppSettings> appSettings)
        {
            _AppSettings = appSettings.Value;
            this._Factory = LogHelper.LoggerFactory;
            this._Logger = this._Factory.CreateLogger("WebHookController");
            this._ClassName = this.GetType().Name;
        }

        // POST: api/Webhook
        [HttpPost]
        public void Post([FromBody]JObject data)
        {
            // Checking if the request payload is null
            if (data != null)
            {
                var webhookSecret = _AppSettings.WebhookSecret;
                var requestSignature = Request.Headers["X-Elli-Signature"].ToString();
                var requestEnvironment = Request.Headers["X-Elli-Environment"].ToString();

                // generate the webhook token from the payload and secret using HMACSHA
                var webHookToken = WebHookHelper.GetWebhookNotificationToken(data.ToString(Formatting.None), webhookSecret);

                _Logger.LogInformation("[" + _ClassName + "] - WebHook Token - " + webHookToken);

                // Check if the generated WebHook token is similar to the request signature received in the header.
                if (WebHookHelper.IsValidWebhookToken(requestSignature, webHookToken))
                {
                    var webHookBody = new WebhookNotificationBody();

                    webHookBody.eventId = data.GetValue<string>("eventId");
                    webHookBody.eventTime = data.GetValue<DateTime>("eventTime");
                    webHookBody.eventType = data.GetValue<string>("eventType");
                    webHookBody.meta = data.GetValue<Meta>("meta");

                    var transactionId = string.Empty;

                    transactionId = webHookBody.meta != null ? webHookBody.meta.resourceId : string.Empty;

                    _Logger.LogInformation("[" + _ClassName + "] - Transaction ID - " + transactionId);
                    _Logger.LogInformation("[" + _ClassName + "] - WebHook Data - " + data.ToString(Formatting.None));

                    
                    var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);

                    // executing the Get Request Partner API Call here
                    var requestData = partnerAPIWrapper.GetRequest(transactionId);

                    if (requestData != null)
                    {
                        var loanInformation = requestData;
                                                
                        // if the requestData is not null then the Partner will validate it against their business rules and if it is valid then they have to build their request here and submit it
                        // the response that they receive from their request will go back into the Partner API as a CreateResponse Partner API call

                        var validationInfo = MockResponseHelper.ValidateLoanData(requestData);

                        if (validationInfo != null && validationInfo["success"] != null)
                        {
                            var response = MockRequestHelper.SubmitToPartner(requestData, transactionId);

                            // This method will build the payload required for Creating Response in the Partner API
                            SubmitAcknowledgementToPartnerAPI(response, loanInformation, transactionId);
                        }
                        else
                            TransactionStatusCache.Instance.Add(transactionId, validationInfo);
                    }
                }
                else
                {
                    _Logger.LogInformation("[" + _ClassName + "] - WebHook Token is Invalid ");
                }
            }
        }

        /// <summary>
        /// This method will build the payload required for Creating Response in the Partner API
        /// </summary>
        /// <param name="response"></param>
        /// <param name="loanInformation"></param>
        /// <param name="transactionId"></param>
        private void SubmitAcknowledgementToPartnerAPI(JObject response, JToken loanInformation, string transactionId)
        {
            if (response != null)
            {
                // assuming the response is of type JToken
                if(response.SelectToken("$.result") != null && loanInformation != null)
                {
                    var result = response.SelectToken("$.result");
                    var productName = loanInformation.SelectToken("$.product.options.productName");
                    var productId = loanInformation.SelectToken("$.product.options.productId");

                    var orderInfo = new OrderInformation()
                    {
                        TransactionId = transactionId,
                        ProductName = productName.ToString(),
                        ProductCode = productId.ToString(),
                        OrderId = result["trackingId"].ToString(),
                        LoanInformation = loanInformation
                    };

                    // Adding Transaction Information in the local In Memory cache. This would be implemented by the partner in their own way to track transactions and their order statuses
                    TransactionInformationCache.Instance.Add(transactionId, orderInfo);

                    // build payload for Partner API Create Response
                    dynamic responsePayload = new JObject();
                    dynamic orders = new JArray();
                    dynamic order = new JObject();

                    order.id = result["trackingId"];
                    order.orderDateTime = result["orderDate"];
                    order.orderStatus = response["status"];
                    order.orderMessage = result["orderMessage"];
                    order.product = productName;

                    orders.Add(order);

                    responsePayload.orders = orders;

                    var responsePayloadString = Convert.ToString(responsePayload);

                    var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);
                    partnerAPIWrapper.CreateResponse(responsePayloadString, transactionId);
                    TransactionStatusCache.Instance.Add(transactionId, response);
                }
            }
        }

    }
}
