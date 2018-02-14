using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Processor.Interfaces
{
    interface IRequestProcessor
    {
        void ProcessWebhookRequest();
    }
}
