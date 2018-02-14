using EPC.ReferenceIntegration.DataStore;
using EPC.ReferenceIntegration.ExtensionMethods;
using EPC.ReferenceIntegration.Helpers;
using EPC.ReferenceIntegration.Models;
using EPC.ReferenceIntegration.Processor.Interfaces;
using EPC.ReferenceIntegration.Wrappers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Processor
{
    public class RequestProcessor : IRequestProcessor
    {
        private string _ClassName = string.Empty;

        private WebhookNotificationBody _WebhookBody { get; set; }
        private ILogger _Logger { get; set; }
        private AppSettings _AppSettings { get; set; }      
        private MockRequestHelper _MockRequestHelper = null;

        public RequestProcessor()
        {

        }
        
        public RequestProcessor(WebhookNotificationBody _WebhookBody, ILogger logger, AppSettings appSettings)
        {
            this._WebhookBody = _WebhookBody;
            this._Logger = logger;
            this._ClassName = this.GetType().Name;
            this._AppSettings = appSettings;
            this._MockRequestHelper = new MockRequestHelper(this._AppSettings);
        }

        /// <summary>
        /// This method will process the webhook request
        /// </summary>
        public void ProcessWebhookRequest()
        {
            var transactionId = this._WebhookBody.meta != null ? _WebhookBody.meta.resourceId : string.Empty;
            var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);

            _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - Transaction ID - " + transactionId);

            // This is for the Transaction messaging flow. if the eventType is NewMessage then the Lender has sent a message to the Partner and the partner has to retreive it
            if (_WebhookBody.eventType == "NewMessage")
            {
                //Dump message body into cache.

                var resourceRefURL = _WebhookBody.meta.resourceRef;
                var messageId = resourceRefURL.Substring(resourceRefURL.LastIndexOf('/') + 1, resourceRefURL.Length - resourceRefURL.LastIndexOf('/') - 1);

                var messageBody = partnerAPIWrapper.GetMessage(transactionId, messageId);
                if (messageBody != null)
                {
                    _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - Message is not null ");
                    MessageCache.Instance.Add(transactionId, messageBody.ToString());
                }
            }
            else if (_WebhookBody.eventType == "CreateRequest")
            {
                // executing the Get Request Partner API Call here
                var requestData = partnerAPIWrapper.GetRequest(transactionId);

                if (requestData != null)
                {
                    _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - Get Request Data is not null ");

                    var loanInformation = requestData;

                    // if the requestData is not null then the Partner will validate it against their business rules and if it is valid then they have to build their request here and submit it
                    // the response that they receive from their request will go back into the Partner API as a CreateResponse Partner API call

                    var mockResponseHelper = new MockResponseHelper(_AppSettings);
                    var validationInfo = mockResponseHelper.ValidateLoanData(requestData);

                    if (validationInfo != null && validationInfo["success"] != null)
                    {
                        // This will get the type of Integration (e.g. Appraisal, Flood, Verification etc.)
                        var integrationCategory = _AppSettings.IntegrationType == null ? "Appraisal" : _AppSettings.IntegrationType;

                        var response = _MockRequestHelper.SubmitToPartner(requestData, transactionId);
                        
                        // This method will build and submit the payload required for Creating the response in EPC
                        var responseParser = new ResponseProcessor(_WebhookBody, _Logger, _AppSettings);

                        // For Data & Docs flow, validating the credentials.
                        if (string.Compare(_AppSettings.IntegrationType, "DataDocs", true) == 0)
                        {
                            // use password manager and do validation. 
                            if (requestData.SelectToken("$.credentials") != null)
                            {
                                var userName = requestData.GetValueByPath<string>("$.credentials.userName");
                                var password = requestData.GetValueByPath<string>("$.credentials.password");

                                // partner will validate the credentials returned from GetRequest with their system and submit the acknowledgement. here we're validating against a mock username and password.
                                if (userName == "datadocs" && password == "#######")
                                    responseParser.SubmitAcknowledgementToEPC(response, loanInformation, "Delivered");
                                else
                                    responseParser.SubmitAcknowledgementToEPC(response, loanInformation, "Rejected"); // if the credentials are invalid or null then send Rejected status
                            }
                            else
                                responseParser.SubmitAcknowledgementToEPC(response, loanInformation, "Rejected"); // if the credentials are invalid or null then send Rejected status
                        }
                        else
                            responseParser.SubmitAcknowledgementToEPC(response, loanInformation); // This is for the other categories.
                    }
                    else
                        TransactionStatusCache.Instance.Add(transactionId, validationInfo);
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
            _Logger.LogInformation("[" + _ClassName + "] - SubmitAcknowledgementToPartnerAPI - [STARTS]");

            try
            {
                if (response != null)
                {
                    _Logger.LogInformation("[" + _ClassName + "] - SubmitAcknowledgementToPartnerAPI - Submitting Response to Partner API ");

                    if (response.SelectToken("$.result") != null && loanInformation != null)
                    {
                        JToken result = response.SelectToken("$.result"); ;
                        JToken productName = null;
                        JToken productId = null;
                        JToken existingOrderId = null;
                        string orderId = "";
                        var random = new System.Random();

                        // building payload for Partner API Create Response
                        dynamic responsePayload = new JObject();
                        dynamic orders = new JArray();
                        dynamic order = null;

                        //var orderDate = DateTime.Now.ToUniversalTime();
                        var orderDate = DateTime.Now;

                        // assuming the response is of type JToken
                        if (string.Compare(_AppSettings.IntegrationType, "Verification", true) == 0)
                        {
                            JToken tokenoptions = loanInformation.SelectToken("$.product.options");

                            if (tokenoptions != null)
                            {
                                foreach (JProperty prop in tokenoptions)
                                {
                                    string sProductName = "";
                                    if (!string.IsNullOrEmpty(prop.Name) && _MockRequestHelper.IsValidProduct(prop.Name))
                                        sProductName = _MockRequestHelper.GetProductDescription(prop.Name);

                                    if (!string.IsNullOrEmpty(sProductName))
                                    {
                                        _Logger.LogInformation("[" + _ClassName + "] - SubmitAcknowledgementToPartnerAPI - ProductName is " + sProductName);

                                        orderId = random.Next(10000, 80000).ToString();

                                        OrderInformation orderInfo = new OrderInformation()
                                        {
                                            TransactionId = transactionId,
                                            ProductName = sProductName,
                                            ProductCode = prop.Name.ToString(),
                                            OrderId = orderId.ToString(),
                                            OrderStatus = response.SelectToken("$.status").ToString(),
                                            LoanInformation = loanInformation
                                        };

                                        TransactionInformationCache.Instance.Add(orderInfo, true);

                                        order = new JObject();
                                        order.id = orderId.ToString();
                                        order.orderDateTime = orderDate.ToString("yyyy-MM-ddTHH:mm-ss:ff"); // result["orderDate"];
                                        order.orderStatus = response["status"];
                                        order.orderMessage = result["orderMessage"];
                                        order.product = sProductName;
                                        order.documents = new JArray();

                                        orders.Add(order);
                                    }
                                }
                            }
                        }
                        else if (string.Compare(_AppSettings.IntegrationType, "DataDocs", true) == 0)
                        {
                            orderId = random.Next(10000, 80000).ToString();

                            // responsePayload = new JObject();
                            //responsePayload.orders = new JArray();
                            dynamic orderResponse = new JObject();
                            orderResponse.id = orderId.ToString();
                            orderResponse.orderDateTime = orderDate.ToString("yyyy-MM-ddTHH:mm-ss:ff"); // result["orderDate"];
                            orderResponse.orderStatus = "Delivered";
                            orderResponse.message = result["orderMessage"];
                            orders.Add(orderResponse);
                        }
                        else
                        {
                            productName = loanInformation.SelectToken("$.product.options.productName");
                            productId = loanInformation.SelectToken("$.product.options.productId");
                            existingOrderId = loanInformation.SelectToken("$.product.options.existingOrderId");

                            // setting the order id to existingOrderId if the payload has it otherwise setting it to a random number
                            orderId = existingOrderId != null ? existingOrderId.ToString() : random.Next(10000, 80000).ToString();

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

                            order = new JObject();
                            order.id = orderId.ToString();
                            order.orderDateTime = orderDate.ToString("yyyy-MM-ddTHH:mm-ss:ff"); // result["orderDate"];
                            order.orderStatus = response["status"];
                            order.orderMessage = result["orderMessage"];
                            order.product = productName;
                            order.documents = new JArray();

                            orders.Add(order);
                        }

                        _Logger.LogInformation("[" + _ClassName + "] - SubmitAcknowledgementToPartnerAPI - Order Date time is " + orderDate.ToString("o"));

                        responsePayload.orders = orders;

                        var responsePayloadString = Convert.ToString(responsePayload);

                        var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);

                        // This is the POST call to the Partner API (Create Response call) | partner/v1/transactions/{{transactionId}}/response
                        var isResponseCreated = partnerAPIWrapper.CreateResponse(responsePayloadString, transactionId);

                        if (isResponseCreated)
                        {
                            _Logger.LogInformation("[" + _ClassName + "] - SubmitAcknowledgementToPartnerAPI - isResponseCreated flag is true");
                            TransactionStatusCache.Instance.Add(transactionId, response);
                        }
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
