using EPC.ReferenceIntegration.Helpers;
using EPC.ReferenceIntegration.Models;
using EPC.ReferenceIntegration.Processor.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using EPC.ReferenceIntegration.DataStore;
using EPC.ReferenceIntegration.Wrappers;

namespace EPC.ReferenceIntegration.Processor
{
    public class ResponseProcessor : IResponseProcessor
    {
        private string _ClassName = string.Empty;
        private WebhookNotificationBody _WebhookBody = null;
        private ILogger _Logger;
        private AppSettings _AppSettings = null;
        private MockRequestHelper _MockRequestHelper = null;

        public ResponseProcessor(WebhookNotificationBody _WebhookBody, ILogger logger, AppSettings appSettings)
        {
            this._WebhookBody = _WebhookBody;
            this._Logger = logger;
            this._ClassName = this.GetType().Name;
            this._AppSettings = appSettings;
            this._MockRequestHelper = new MockRequestHelper(this._AppSettings);
        }

        /// <summary>
        /// This method will submit partner acknowledgement to EPC
        /// </summary>
        /// <param name="response"></param>
        /// <param name="loanInformation"></param>
        public void SubmitAcknowledgementToEPC(JObject response, JToken loanInformation, string status = "")
        {
            dynamic responsePayload = new JObject();
            dynamic orders = new JArray();
            var transactionId = _WebhookBody.meta != null ? _WebhookBody.meta.resourceId : string.Empty;

            if (response != null)
            {
                _Logger.LogInformation("[" + _ClassName + "] - SubmitAcknowledgementToEPC - Submitting Response to Partner API ");

                if (response.SelectToken("$.result") != null && loanInformation != null)
                {
                    if (string.Compare(_AppSettings.IntegrationType, "Appraisal", true) == 0)
                        orders = ProcessAppraisalResponse(response, loanInformation);
                    else if (string.Compare(_AppSettings.IntegrationType, "Verification", true) == 0)
                        orders = ProcessVerificationResponse(response, loanInformation);
                    else if (string.Compare(_AppSettings.IntegrationType, "DataDocs", true) == 0)
                        orders = ProcessDataDocsResponse(response, status);

                    responsePayload.orders = orders;

                    var responsePayloadString = Convert.ToString(responsePayload);

                    var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);

                    // This is the POST call to the Partner API (Create Response call) | partner/v1/transactions/{{transactionId}}/response
                    var isResponseCreated = partnerAPIWrapper.CreateResponse(responsePayloadString, transactionId);

                    if (isResponseCreated)
                    {
                        _Logger.LogInformation("[" + _ClassName + "] - SubmitAcknowledgementToEPC - isResponseCreated flag is true");
                        TransactionStatusCache.Instance.Add(transactionId, response);
                    }
                }
            }
        }

        #region " Private Methods "

        /// <summary>
        /// This method will process and return Mocked Appraisal Response for EPC
        /// </summary>
        /// <param name="response"></param>
        /// <param name="loanInformation"></param>
        /// <returns></returns>
        private JArray ProcessAppraisalResponse(JObject response, JToken loanInformation)
        {
            _Logger.LogInformation("[" + _ClassName + "] - ProcessAppraisalResponse - [STARTS]");

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

            productName = loanInformation.SelectToken("$.product.options.productName");
            productId = loanInformation.SelectToken("$.product.options.productId");
            existingOrderId = loanInformation.SelectToken("$.product.options.existingOrderId");

            // setting the order id to existingOrderId if the payload has it otherwise setting it to a random number
            orderId = existingOrderId != null ? existingOrderId.ToString() : random.Next(10000, 80000).ToString();

            var orderInfo = new OrderInformation()
            {
                TransactionId = _WebhookBody.meta != null ? _WebhookBody.meta.resourceId : string.Empty,
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

            _Logger.LogInformation("[" + _ClassName + "] - ProcessAppraisalResponse - [STARTS]");

            return orders;
        }

        /// <summary>
        /// This method will process and return Mocked Verification Response for EPC
        /// </summary>
        /// <param name="response"></param>
        /// <param name="loanInformation"></param>
        private JArray ProcessVerificationResponse(JObject response, JToken loanInformation)
        {
            _Logger.LogInformation("[" + _ClassName + "] - ProcessVerificationResponse - [STARTS]");

            JToken result = response.SelectToken("$.result");
            string orderId = "";
            var random = new System.Random();
            JToken tokenoptions = loanInformation.SelectToken("$.product.options");
            // building payload for Partner API Create Response
            dynamic responsePayload = new JObject();
            dynamic orders = new JArray();
            dynamic order = null;

            //var orderDate = DateTime.Now.ToUniversalTime();
            var orderDate = DateTime.Now;

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
                            TransactionId = _WebhookBody.meta != null ? _WebhookBody.meta.resourceId : string.Empty,
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

            _Logger.LogInformation("[" + _ClassName + "] - ProcessVerificationResponse - [ENDS]");

            return orders;
        }

        /// <summary>
        /// This method will process and return Mocked Data & Docs Response for EPC
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private JArray ProcessDataDocsResponse(JObject response, string status)
        {
            _Logger.LogInformation("[" + _ClassName + "] - ProcessDataDocsResponse - [STARTS]");

            JToken result = response.SelectToken("$.result");
            var random = new System.Random();
            // building payload for Partner API Create Response
            dynamic responsePayload = new JObject();
            dynamic orders = new JArray();
            var orderDate = DateTime.Now;

            var orderId = random.Next(10000, 80000).ToString();

            var message = string.Empty;

            if (string.Compare(_AppSettings.IntegrationType, "Rejected", true) == 0)
                message = "The transaction was rejected since the credentials are invalid";
            else
                message = "The transaction has been delivered.";

            dynamic orderResponse = new JObject();
            orderResponse.id = orderId.ToString();
            orderResponse.orderDateTime = orderDate.ToString("yyyy-MM-ddTHH:mm-ss:ff"); 
            orderResponse.orderStatus = status;
            orderResponse.message = message;
            orders.Add(orderResponse);

            _Logger.LogInformation("[" + _ClassName + "] - ProcessDataDocsResponse - [ENDS]");

            return orders;
        }

        #endregion

    }
}
