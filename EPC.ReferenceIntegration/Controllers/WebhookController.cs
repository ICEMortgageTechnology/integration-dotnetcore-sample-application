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
using EPC.ReferenceIntegration.Processor;

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
        private MockRequestHelper _MockRequestHelper = null;

        public WebHookController(IOptions<AppSettings> appSettings)
        {
            _AppSettings = appSettings.Value;
            this._Factory = LogHelper.LoggerFactory;
            this._Logger = this._Factory.CreateLogger("WebHookController");
            this._ClassName = this.GetType().Name;

            _MockRequestHelper = new MockRequestHelper(this._AppSettings);
        }

        /// <summary>
        /// This API is a callback to receive webhook notifications from the PartnerAPI
        /// </summary>
        /// <param name="data"></param>
        // POST: api/Webhook
        [HttpPost]
        public IActionResult Post([FromBody]JObject data)
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

                    var requestProcessor = new RequestProcessor(webHookBody, _Logger, _AppSettings);

                    // processing the webhook request here.
                    requestProcessor.ProcessWebhookRequest();

                }
                else
                    _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - WebHook Token is Invalid ");
            }

            _Logger.LogInformation("[" + _ClassName + "] - POST - api/webhook - [ENDS]");

            return Ok();
        }
        

    
    }
}
