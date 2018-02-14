using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using EPC.ReferenceIntegration.Helpers;
using EPC.ReferenceIntegration.Wrappers.Interfaces;
using EPC.ReferenceIntegration.Wrappers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EPC.ReferenceIntegration.ExtensionMethods;

/// <summary>
/// This controller is for Create Request
/// </summary>
namespace EPC.ReferenceIntegration.Controllers
{
    [Route("api/[controller]")]
    public class RequestController : Controller
    {
        private readonly AppSettings _AppSettings;
        private ILoggerFactory _Factory;
        private ILogger _Logger;

        public RequestController(IOptions<AppSettings> appSettings)
        {
            _AppSettings = appSettings.Value;
            this._Factory = LogHelper.LoggerFactory;
            this._Logger = this._Factory.CreateLogger("RequestController");
        }

        /// <summary>
        /// This API is for the Create Request call. It is called when the Submit Order button is pressed on the UI
        /// </summary>
        /// <param name="data"></param>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        // POST api/request
        [HttpPost]
        [Route("{transactionId}")]
        public IActionResult Post([FromBody]JObject data, string transactionId)
        {
            try
            {
                _Logger.LogInformation("[RequestController] - POST - api/request - [STARTS]");

                // checking to see if the incoming data is a Valid JSON and does not contain any XSS
                if (!data.IsValidJSON())
                {
                    _Logger.LogInformation("[RequestController] - POST - api/request - Incoming JSON is invalid");
                    _Logger.LogInformation("[RequestController] - POST - api/request - " + data.ToString());
                    return BadRequest("The JSON payload is Invalid. Please provide a valid Input.");
                }

                // checking if the incoming data is null
                if (data != null)
                {
                    var requestObject = new JObject(data);

                    

                    //// Sanitizing the Payload here for any html or script tags and escaping them
                    
                    var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);

                    // This is the POST call to the Partner API (Create Request call) | // partner/v1/transactions/{{transactionId}}/request
                    partnerAPIWrapper.CreateRequest(requestObject.ToString(Formatting.None), transactionId);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[RequestController] - POST - Error - " + ex.Message);
                _Logger.LogError("[RequestController] - POST - Stack Trace - " + ex.StackTrace);

                return StatusCode(500);
            }

            _Logger.LogInformation("[RequestController] - POST - api/request - [ENDS]");
            return Ok("{'success': true}");
        }

        /// <summary>
        /// This API is for the Update Request call. It is called when the Send button is pressed on the UI
        /// </summary>
        /// <param name="data"></param>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        // POST api/request
        [HttpPost]
        [Route("{transactionId}/attachments")]
        public IActionResult UpdateRequest([FromBody]JObject data, string transactionId)
        {
            try
            {
                _Logger.LogInformation("[RequestController] - Update - api/request - [STARTS]");

                // checking to see if the incoming data is a Valid JSON and does not contain any XSS
                if (!data.IsValidJSON())
                {
                    _Logger.LogInformation("[RequestController] - Update - api/request - Incoming JSON is invalid");
                    _Logger.LogInformation("[RequestController] - Update - api/request - " + data.ToString());
                    return BadRequest("The JSON payload is Invalid. Please provide a valid Input.");
                }

                // checking if the incoming data is null
                if (data != null)
                {
                    var requestObject = new JObject(data);

                    //// Sanitizing the Payload here for any html or script tags and escaping them

                    var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);

                    // This is the POST call to the Partner API (Create Request call) | // partner/v1/transactions/{{transactionId}}/request
                    partnerAPIWrapper.UpdateRequest(requestObject.ToString(Formatting.None), transactionId);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[RequestController] - POST - Error - " + ex.Message);
                _Logger.LogError("[RequestController] - POST - Stack Trace - " + ex.StackTrace);

                return StatusCode(500);
            }

            _Logger.LogInformation("[RequestController] - POST - api/request - [ENDS]");
            return Ok("{'success': true}");
        }

    }
}
