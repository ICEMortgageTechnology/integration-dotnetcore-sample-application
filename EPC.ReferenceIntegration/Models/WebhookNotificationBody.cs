using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Models
{
    public class WebhookNotificationBody
    {
        public WebhookNotificationBody()
        {
            meta = new Meta();
        }

        public string eventId { get; set; }
        public DateTime eventTime { get; set; }
        public string eventType { get; set; }
        public Meta meta { get; set; }
    }

    public class Meta
    {
        public string userId { get; set; }
        public string resourceId { get; set; }
        public string resourceType { get; set; }
        public string resourceRef { get; set; }
    }


}
