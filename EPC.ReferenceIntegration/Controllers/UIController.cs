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
        public JObject Get(string transactionId)
        {
            JObject uiDataResponseJson = null;

            var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);

            // makes the Get Origin call from the Partner API
            var originResponse = partnerAPIWrapper.GetOrigin(transactionId);

            if(originResponse != null)
            {
                uiDataResponseJson = new JObject();
                uiDataResponseJson.Add("originResponse", originResponse);
            }            

            return uiDataResponseJson;
        }

        [HttpPost]
        [Route("{transactionId}/{type}")]
        public JObject Post([FromBody]JObject data, string transactionId, string type)
        {
            JObject uiDataResponseJson = null;

            if (data != null)
            {
                // the partner will do their own authentication and authorization here
                // this is just a demo 
                if(data["userName"] != null && data.GetValue("userName").ToString() == "test@testappraisal.com" && data["password"] != null && data.GetValue("password").ToString() == "password")
                {
                    if (MockResponseHelper.GetUIDataResponse() != null)
                    {
                        uiDataResponseJson = MockResponseHelper.GetUIDataResponse();
                    }
                }
            }

            return uiDataResponseJson;
        }
        
    }
}
