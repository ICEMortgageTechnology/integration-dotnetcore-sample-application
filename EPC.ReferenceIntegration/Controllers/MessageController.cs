using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using EPC.ReferenceIntegration.Helpers;
using EPC.ReferenceIntegration.DataStore;
using EPC.ReferenceIntegration.Wrappers.Interfaces;
using EPC.ReferenceIntegration.Wrappers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EPC.ReferenceIntegration.ExtensionMethods;

namespace EPC.ReferenceIntegration.Controllers
{
    [Produces("application/json")]
    [Route("api/Message")]
    public class MessageController : Controller
    {
        private readonly AppSettings _AppSettings;
        private ILoggerFactory _Factory;
        private ILogger _Logger;
        public MessageController(IOptions<AppSettings> appSettings)
        {
            _AppSettings = appSettings.Value;
            this._Factory = LogHelper.LoggerFactory;
            this._Logger = this._Factory.CreateLogger("MessageController");
        }
        // POST api/message/{transactionId}
        [HttpPost]
        [Route("{transactionId}")]
        public IActionResult Post([FromBody]JObject data, string transactionId)
        {
            try
            {
                _Logger.LogInformation("[MessageController] - POST - api/message - [STARTS]");

                // checking to see if the incoming data is a Valid JSON and does not contain any XSS
                if (!data.IsValidJSON())
                {
                    _Logger.LogInformation("[MessageController] - POST - api/message - Incoming JSON is invalid");
                    _Logger.LogInformation("[MessageController] - POST - api/message - " + data.ToString());
                    return BadRequest("The JSON payload is Invalid. Please provide a valid Input.");
                }

                // checking if the incoming data is null
                if (data != null)
                {
                    var requestObject = new JObject(data);
                    
                    var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);
                    
                   var result = partnerAPIWrapper.PostMessage(transactionId, requestObject.ToString(Formatting.None));
                    if (result)
                    {
                        return Ok();
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[MessageController] - POST - Error - " + ex.Message);
                _Logger.LogError("[MessageController] - POST - Stack Trace - " + ex.StackTrace);

                return StatusCode(500);
            }

            _Logger.LogInformation("[MessageController] - POST - api/message - [ENDS]");
            return StatusCode(400);
        }

        // GET api/message/{transactionId}/all
        // Direct API call to fetch all messages
        [HttpGet]
        [Route("{transactionId}/all")]
        public IActionResult GetAll(string transactionId)
        {
            try
            {
                _Logger.LogInformation("[MessageController] - GET - api/message - [STARTS]");
                var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);
                JObject result = partnerAPIWrapper.GetAllMessages(transactionId);
                if(result != null)
                {
                    //build response
                    return Ok(result);
                }
                else
                {
                    //build error response
                    return NoContent();
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[MessageController] - GET - Error - " + ex.Message);
                _Logger.LogError("[MessageController] - GET - Stack Trace - " + ex.StackTrace);

                return StatusCode(500);
            }
        }
        // GET api/message/{transactionId}
        // Pull latest message received from webhook
        [HttpGet]
        [Route("{transactionId}")]
        public IActionResult Get(string transactionId)
        {

            dynamic message = new JObject();

            // checking the TransactionStatusCache to see if there is a status for the given transactionId. In real world scenarios, this could come from the DB or any data store at the partner's end.
            var retrieved = MessageCache.Instance.GetValue(transactionId);

            if (retrieved != null)
            {
                _Logger.LogInformation("[MessageController] - GET - api/messages - [STARTS]");
                message = JsonConvert.DeserializeObject(retrieved);
                // Once the Transaction Status is read from the Cache, we need to remove the TransactionID from the cache
                MessageCache.Instance.Remove(transactionId);
                _Logger.LogInformation("[MessageController] - GET - api/messages - [ENDS]");
                return Ok(message);
            }
            else
                return NoContent();

        }
    }
    
    }