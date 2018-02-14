using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using EPC.ReferenceIntegration.DataStore;
using EPC.ReferenceIntegration.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EPC.ReferenceIntegration.Wrappers;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;

/// <summary>
/// This controller is for checking the order Acknowledgement and status of an order 
/// </summary>
namespace EPC.ReferenceIntegration.Controllers
{
    [Produces("application/json")]
    [Route("api/Status")]
    public class StatusController : Controller
    {
        private readonly AppSettings _AppSettings;
        private ILoggerFactory _Factory;
        private ILogger _Logger;
        private string _ClassName = string.Empty;

        public StatusController(IOptions<AppSettings> appSettings)
        {
            _AppSettings = appSettings.Value;
            this._Factory = LogHelper.LoggerFactory;
            this._Logger = this._Factory.CreateLogger("StatusController");
            this._ClassName = this.GetType().Name;
        }

        /// <summary>
        /// This API will be used by the UI to check for initial Acknowledgement. 
        /// It is called when the Submit Order Response is successful and we need to check for an Acknowledgement
        /// This is called from a javascript timer.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        // GET: api/Status/{transactionId}
        [HttpGet("{transactionId}")]
        public JObject Get(string transactionId)
        {
            _Logger.LogInformation("[StatusController] - GET - api/status - [STARTS]");

            JObject status = new JObject();

            // checking the TransactionStatusCache to see if there is a status for the given transactionId. In real world scenarios, this could come from the DB or any data store at the partner's end.
            var transactionStatus = TransactionStatusCache.Instance.GetValue(transactionId);
            
            if(transactionStatus != null)
            {
                status.Add("status", JToken.FromObject(transactionStatus));

                // Once the Transaction Status is read from the Cache, we need to remove the TransactionID from the cache
                TransactionStatusCache.Instance.Remove(transactionId);
            }                
            else
                status.Add("status", JToken.FromObject("Not Found"));

            _Logger.LogInformation("[StatusController] - GET - api/status - [ENDS]");

            return status;
        }

        /// <summary>
        /// This API will be used for the Check Status call from the Order Tab
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpPost("{transactionId}/{orderId}")]
        public IActionResult Post(string transactionId, string orderId)
        {
            JObject status = new JObject();

            try
            {
                _Logger.LogInformation("[StatusController] - POST - api/status - [STARTS]");
                
                // partner will check their systems/repository to check the status of the current order and return the documents if it is completed.
                var mockResponseHelper = new MockResponseHelper(_AppSettings);
                var checkStatusResponse = mockResponseHelper.GetResponseForCheckStatus(transactionId, orderId);

                if (checkStatusResponse != null)
                {
                    var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);
                    var responseString = Convert.ToString(checkStatusResponse);

                    // This is the POST call to the Partner API (Create Response call) | partner/v1/transactions/{{transactionId}}/response
                    var isResponseCreated = partnerAPIWrapper.CreateResponse(responseString, transactionId);

                    if (isResponseCreated)
                    {
                        _Logger.LogInformation("[StatusController] - POST - api/status - Response was created.");

                        // building success response for UI
                        var result = new JObject();
                        var orderDate = DateTime.Now;

                        status.Add("status", checkStatusResponse["status"]);
                        result.Add("trackingId", orderId);
                        result.Add("orderDate", orderDate.ToString("yyyy-MM-ddTHH:mm-ss:ff"));
                        result.Add("orderMessage", "Order has been completed successfully.");

                        status.Add("result", result);

                        // updating the transaction status in the memory cache so that the status check polling method on the UI gets the updated status of the transaction.
                        TransactionStatusCache.Instance.Add(transactionId, status);
                    }
                    else
                    {
                        _Logger.LogInformation("[StatusController] - POST - api/status - Response was not Created. isResponseCreate flag is false.");

                        // return Internal Server Error
                        return StatusCode(500);
                    }
                }
                else
                {
                    _Logger.LogInformation("[StatusController] - POST - api/status - checkStatusResponse is null.");

                    // return Internal Server Error
                    return StatusCode(500);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[StatusController] - POST - Error - " + ex.Message);
                return StatusCode(500);
            }

            _Logger.LogInformation("[StatusController] - POST - api/status - [ENDS]");
            return Ok(status);
        }
    }
}
