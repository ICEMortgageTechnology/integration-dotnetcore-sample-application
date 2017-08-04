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

/// <summary>
/// This is the controlller class for Webhook notifications
/// </summary>
namespace EPC.ReferenceIntegration.Controllers
{
    [Produces("application/json")]
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

        /// <summary>
        /// This API is a callback to receive webhook notifications from the PartnerAPI
        /// </summary>
        /// <param name="data"></param>
        // POST: api/Webhook
        [HttpPost]
        public void Post([FromBody]JObject data)
        {
            _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - [STARTS]");

            // Checking if the request payload is null
            if (data != null)
            {
                _Logger.LogInformation("[" + _ClassName + "]  - POST - api/webhook -  Received Webhook Notification at ");

                var webhookSecret = _AppSettings.WebhookSecret;
                var requestSignature = Request.Headers["Elli-Signature"].ToString();
                var requestEnvironment = Request.Headers["Elli-Environment"].ToString();

                _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - Request Elli-Signature - " + requestSignature);
                _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - Request Elli-Environment - " + requestEnvironment);

                // generate the webhook token from the payload and secret using HMACSHA
                var webHookToken = WebHookHelper.GetWebhookNotificationToken(data.ToString(Formatting.None), webhookSecret);

                _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - WebHook Data - " + data.ToString(Formatting.Indented));
                _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - WebHook Token - " + webHookToken);

                // Check if the generated WebHook token is similar to the request signature received in the header.
                if (WebHookHelper.IsValidWebhookToken(requestSignature, webHookToken))
                {
                    var webHookBody = new WebhookNotificationBody()
                    {
                        eventId = data.GetValue<string>("eventId"),
                        eventTime = data.GetValue<DateTime>("eventTime"),
                        eventType = data.GetValue<string>("eventType"),
                        meta = data.GetValue<Meta>("meta")
                    };

                    var transactionId = webHookBody.meta != null ? webHookBody.meta.resourceId : string.Empty;

                    _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - Transaction ID - " + transactionId);
                    
                    var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);

                    // executing the Get Request Partner API Call here
                    var requestData = partnerAPIWrapper.GetRequest(transactionId);

                    if (requestData != null)
                    {
                        _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - Get Request Data is not null ");

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
                    _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - WebHook Token is Invalid ");
            }

            _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - [ENDS]");
        }

        /// <summary>
        /// This method will build the payload required for Creating Response in the Partner API
        /// </summary>
        /// <param name="response"></param>
        /// <param name="loanInformation"></param>
        /// <param name="transactionId"></param>
        private void SubmitAcknowledgementToPartnerAPI(JObject response, JToken loanInformation, string transactionId)
        {
            _Logger.LogInformation("[" + _ClassName + "] - SubmitAcknowledgementToPartnerAPI - [STARTS]");

            try
            {
                if (response != null)
                {
                    _Logger.LogInformation("[" + _ClassName + "] - SubmitAcknowledgementToPartnerAPI - Submitting Resposne to Partner API ");

                    // assuming the response is of type JToken
                    if (response.SelectToken("$.result") != null && loanInformation != null)
                    {
                        var result = response.SelectToken("$.result");
                        var productName = loanInformation.SelectToken("$.product.options.productName");
                        var productId = loanInformation.SelectToken("$.product.options.productId");
                        var existingOrderId = loanInformation.SelectToken("$.product.options.existingOrderId");

                        var random = new System.Random();

                        // setting the order id to existingOrderId if the payload has it otherwise setting it to a random number
                        var orderId = existingOrderId != null ? existingOrderId.ToString() : random.Next(10000, 80000).ToString();

                        var orderInfo = new OrderInformation()
                        {
                            TransactionId = transactionId,
                            ProductName = productName != null ? productName.ToString() : string.Empty,
                            ProductCode = productId != null ? productId.ToString() : string.Empty,
                            OrderId = existingOrderId != null ? existingOrderId.ToString() : orderId.ToString(), // adding existing order id if it exists in the response object,
                            OrderStatus = response.SelectToken("$.status").ToString(),
                            LoanInformation = loanInformation
                        };

                        // Adding Transaction Information in the local In Memory cache. This would be implemented by the partner in their own way to track transactions and their order statuses
                        TransactionInformationCache.Instance.Add(orderInfo);

                        // building payload for Partner API Create Response
                        dynamic responsePayload = new JObject();
                        dynamic orders = new JArray();
                        dynamic order = new JObject();
                        
                        //var orderDate = DateTime.Now.ToUniversalTime();
                        var orderDate = DateTime.Now;

                        _Logger.LogInformation("[" + _ClassName + "] - SubmitAcknowledgementToPartnerAPI - Order Date time is " + orderDate.ToString("o"));

                        order.id = orderId.ToString();
                        order.orderDateTime = orderDate.ToString("yyyy-MM-ddTHH:mm-ss:ff"); // result["orderDate"];
                        order.orderStatus = response["status"];
                        order.orderMessage = result["orderMessage"];
                        order.product = productName;
                        order.documents = new JArray();

                        orders.Add(order);

                        responsePayload.orders = orders;

                        var responsePayloadString = Convert.ToString(responsePayload);
                        
                        var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);

                        // This is the POST call to the Partner API (Create Response call) | partner/v1/transactions/{{transactionId}}/response
                        var isResponseCreated = partnerAPIWrapper.CreateResponse(responsePayloadString, transactionId);

                        if(isResponseCreated)
                            TransactionStatusCache.Instance.Add(transactionId, response);
                    }
                }
            }
            catch (Exception ex) 
            {
                _Logger.LogError("[" + _ClassName + "] - SubmitAcknowledgementToPartnerAPI - Exception - " + ex.Message);
                _Logger.LogError("[" + _ClassName + "] - SubmitAcknowledgementToPartnerAPI - StackTrace - " + ex.StackTrace);
            }

            _Logger.LogInformation("[" + _ClassName + "] - SubmitAcknowledgementToPartnerAPI - [ENDS]");
        }
    }
}
