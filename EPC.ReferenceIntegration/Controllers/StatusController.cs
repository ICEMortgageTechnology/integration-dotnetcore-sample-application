using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using EPC.ReferenceIntegration.DataStore;

namespace EPC.ReferenceIntegration.Controllers
{
    [Produces("application/json")]
    [Route("api/Status")]
    public class StatusController : Controller
    {
        // GET: api/Status/{transactionId}
        [HttpGet("{transactionId}")]
        public JObject Get(string transactionId)
        {
            JObject status = new JObject();

            var transactionStatus = TransactionStatusCache.Instance.GetValue(transactionId);
            
            if(transactionStatus != null)
            {
                status.Add("status", JToken.FromObject(transactionStatus));

                // Once the Transaction Status is read from the Cache, we need to remove the TransactionID from the cache
                TransactionStatusCache.Instance.Remove(transactionId);
            }                
            else
                status.Add("status", JToken.FromObject("Not Found"));

            return status;
        }
    }
}
