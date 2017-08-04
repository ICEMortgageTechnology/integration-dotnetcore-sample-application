using EPC.ReferenceIntegration.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Wrappers.Interfaces
{
    public interface IOAuthWrapper
    {
        /// <summary>
        /// Gets the access token based on OAuth Credentials
        /// </summary>
        /// <returns></returns>
        Token GetOAuthAccessToken();

        JObject IntrospectAccessToken();

    }
}
