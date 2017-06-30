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

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

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

        // POST api/request
        [HttpPost]
        [Route("{transactionId}")]
        public IActionResult Post([FromBody]JObject data, string transactionId)
        {
            try
            {
                if (data != null)
                {
                    var requestObject = new JObject(data);

                    var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);
                    partnerAPIWrapper.CreateRequest(requestObject.ToString(Formatting.None), transactionId);
                    
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[RequestController] - POST - Error - " + ex.Message);
                _Logger.LogError("[RequestController] - POST - Stack Trace - " + ex.StackTrace);

                return StatusCode(500);
            }

            return Ok("{'success': true}");
        }

        // PUT api/request/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/request/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
