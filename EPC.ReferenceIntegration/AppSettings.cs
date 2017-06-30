using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration
{
    public class AppSettings
    {
        public string WebhookSecret { get; set; }
        public string OAuthTokenEndPoint { get; set; }
        public string APIHost { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Scope { get; set; }
        public string PartnerClientID { get; set; }
        public string PartnerClientSecret { get; set; }
        public string WebhookURL { get; set; }
        public string WebhookSubscriptionID { get; set; }
        public string WebhookEndPoint { get; set; }
        public string PartnerJWT { get; set; }
        public PartnerAPI PartnerAPI { get; set; }
    }

    public class PartnerAPI
    {
        public string EndPoint { get; set; }
        public string OriginURI { get; set; }
        public string RequestURI { get; set; }
        public string ResponseURI { get; set; }
        public string DropFilesURI { get; set; }
    }
}
