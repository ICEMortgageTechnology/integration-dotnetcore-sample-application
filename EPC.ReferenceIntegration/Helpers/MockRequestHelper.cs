using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Helpers
{
    public class MockRequestHelper
    {
        /// <summary>
        /// This method is a placeholder for the Partner to convert the Elliemae Response to their specific payload
        /// </summary>
        /// <param name="data"></param>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public static JObject SubmitToPartner(JObject data, string transactionId)
        {
            JObject partnerResponse = new JObject();
            
            var mockSuccessResponsePath = System.IO.Path.GetFullPath(System.IO.Directory.GetCurrentDirectory() + @"\ResponsePayloads\PartnerSuccessResponse.json");
            var successResponse = System.IO.File.ReadAllText(mockSuccessResponsePath);

            partnerResponse = JObject.Parse(successResponse);

            return partnerResponse;
        }
    }
}
