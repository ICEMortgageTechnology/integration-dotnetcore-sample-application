using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Models
{
    public class OAuthKeys
    {
        public string OAuthEndPoint { get; set; }
        public string APIHost { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Scope { get; set; }
        public string PartnerClientID { get; set; }
        public string PartnerClientSecret { get; set; }
        public string WebhookSubscriptionID { get; set; }
        public string WebhookEndPoint { get; set; }
    }
}
