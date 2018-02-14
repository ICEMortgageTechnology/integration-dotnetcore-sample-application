using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using EPC.ReferenceIntegration.Helpers;
using System.Net.Http;
using System.Net;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using EPC.ReferenceIntegration.Wrappers;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace EPC.ReferenceIntegration.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class UIController : Controller
    {
        private readonly AppSettings _AppSettings;
        private ILoggerFactory _Factory;
        private ILogger _Logger;

        public UIController(IOptions<AppSettings> appSettings)
        {
            _AppSettings = appSettings.Value;
            this._Factory = LogHelper.LoggerFactory;
            this._Logger = this._Factory.CreateLogger("UIController");
        }

        // GET api/ui/5
        [HttpGet("{transactionId}")]
        public string Get(string transactionId)
        {
            _Logger.LogInformation("[UIController] - GET - api/ui - [STARTS]");

            string uiResponse = string.Empty;

            var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);

            // makes the Get Origin call from the Partner API
            var originResponse = partnerAPIWrapper.GetOrigin(transactionId);

            if(originResponse != null)
            {
                _Logger.LogInformation("[UIController] - GET - api/ui - OriginResponse is not null");

                dynamic uiDataResponseJson = new JObject();
                uiDataResponseJson.Add("originResponse", originResponse);

                if(uiDataResponseJson.originResponse.credentials != null)
                { 
                    // reading the credentials object and stripping the password before sending it to the ui
                    var credentials = uiDataResponseJson.originResponse.credentials;

                    // better to have a universal password naming convention here
                    credentials["133003.Pwd"] = "********";
                    credentials["password"] = "********";

                }

                // Sanitizing the response payload here for any html or script tags and escaping them. 
                var settings = new JsonSerializerSettings()
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                };

                uiResponse = JsonConvert.SerializeObject(uiDataResponseJson, settings);

                //uiResponse = uiDataResponseJson.ToString(Formatting.None);
            }

            _Logger.LogInformation("[UIController] - GET - api/ui - [END]");

            return uiResponse;
        }

        [HttpPost]
        [Route("{transactionId}/{type}")]
        public string Post([FromBody]JObject data, string transactionId, string type)
        {
            _Logger.LogInformation("[UIController] - POST - api/ui - [STARTS]");

            JObject uiDataResponseJson = null;
            string uiResponse = string.Empty;

            if (data != null)
            {
                // the partner will do their own authentication and authorization here
                // this is just a demo 
                if(data["userName"] != null && data.GetValue("userName").ToString() == "ellietest@testappraisal.com" && data["password"] != null && data.GetValue("password").ToString() == "######")
                {
                    var mockResponseHelper = new MockResponseHelper(_AppSettings);

                    uiDataResponseJson = mockResponseHelper.GetUIDataResponse();

                    if (uiDataResponseJson != null)
                    {
                        // Sanitizing the response payload here for any html or script tags and escaping them.
                        var settings = new JsonSerializerSettings()
                        {
                            StringEscapeHandling = StringEscapeHandling.EscapeHtml
                        };

                        uiResponse = JsonConvert.SerializeObject(uiDataResponseJson, settings);
                        
                    }
                }
            }

            _Logger.LogInformation("[UIController] - POST - api/ui - [ENDS]");

            return uiResponse;
        }
        
    }
}
