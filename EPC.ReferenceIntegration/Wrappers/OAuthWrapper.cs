using System.Collections.Generic;
using EPC.ReferenceIntegration.Models;
using Newtonsoft.Json.Linq;
using EPC.ReferenceIntegration.Helpers;
using EPC.ReferenceIntegration.Wrappers.Interfaces;

namespace EPC.ReferenceIntegration.Wrappers
{
    public class OAuthWrapper : IOAuthWrapper
    {
        private IEnumerable<KeyValuePair<string, string>> _OAuthCredentials = null;
        private string _OAuthURL = string.Empty;
        private string _EndPoint = string.Empty;        

        /// <summary>
        /// Public constructor that takes OAuthKeys model
        /// </summary>
        /// <param name="oAuthKeys"></param>
        public OAuthWrapper(Dictionary<string, string> oAuthKeys, string oAuthURL, string endPoint)
        {
            _OAuthCredentials = oAuthKeys;

            if (!string.IsNullOrEmpty(oAuthURL))
                this._OAuthURL = string.Format("https://{0}", oAuthURL);

            if (!string.IsNullOrEmpty(endPoint))
                this._EndPoint = endPoint;
        }        

        /// <summary>
        /// Gets the OAuth Access token based on the credentials passed
        /// </summary>
        /// <returns></returns>
        public Token GetOAuthAccessToken()
        {
            Token responseToken = null;

            // getting the response string by performing an Http Post
            //var responseString = HttpCommunicationHelper.PerformPost(_OAuthCredentials, this._OAuthURL, this._EndPoint);
            var responseString = HttpCommunicationHelper.ExecuteRequest(_OAuthCredentials, this._OAuthURL, this._EndPoint, RestSharp.Method.POST);

            if (!string.IsNullOrEmpty(responseString))
            {
                var jsonObject = JObject.Parse(responseString);

                // parsing the response if it is not null
                if (jsonObject != null)
                {
                    responseToken = new Token()
                    {
                        TokenString = jsonObject.GetValue("access_token").ToString(),
                        TokenType = jsonObject.GetValue("token_type").ToString()
                    };
                }
            }

            return responseToken;
        }

        /// <summary>
        /// Will return the header that is required for generating the OAuth Token from OAPI
        /// </summary>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetOAuthHeaders(AppSettings appSettings)
        {
            var OAuthKeyDictionary = new Dictionary<string, string>();           

            if(appSettings != null)
            {
                //OAuthKeyDictionary.Add("api_host", appSettings.APIHost);
                OAuthKeyDictionary.Add("client_id", appSettings.ClientID);
                OAuthKeyDictionary.Add("client_secret", appSettings.ClientSecret);
                OAuthKeyDictionary.Add("grant_type", "client_credentials");
                OAuthKeyDictionary.Add("scope", appSettings.Scope);
            }

            return OAuthKeyDictionary;
        }

        /// <summary>
        /// Will Introspect the token
        /// </summary>
        /// <returns></returns>
        public JObject IntrospectAccessToken()
        {
            // getting the response string by performing an Http Post
            var responseString = HttpCommunicationHelper.ExecuteRequest(_OAuthCredentials, this._OAuthURL, this._EndPoint, RestSharp.Method.POST);

            var jsonObject = JObject.Parse(responseString);

            return jsonObject;
        }
    }
}
