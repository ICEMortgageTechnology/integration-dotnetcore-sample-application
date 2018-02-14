using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Processor.Interfaces
{
    interface IResponseProcessor
    {
        void SubmitAcknowledgementToEPC(JObject response, JToken loanInformation, string status = "");
    }
}
