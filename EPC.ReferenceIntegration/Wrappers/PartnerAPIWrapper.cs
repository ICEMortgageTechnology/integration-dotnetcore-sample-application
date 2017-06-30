using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using EPC.ReferenceIntegration.Wrappers.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using EPC.ReferenceIntegration.Models;
using EPC.ReferenceIntegration.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using EPC.ReferenceIntegration.DataStore;
using System.Net;

namespace EPC.ReferenceIntegration.Wrappers
{
    public class PartnerAPIWrapper : IPartnerAPI
    {
        private string _PartnerAPIURL = string.Empty;
        private string _EndPoint = string.Empty;
        private AppSettings _AppSettings = null;
        private ILoggerFactory _Factory;
        private ILogger _Logger;

        public PartnerAPIWrapper(AppSettings appsettings)
        {
            this._AppSettings = appsettings;
            this._Factory = LogHelper.LoggerFactory;
            this._Logger = this._Factory.CreateLogger("PartnerAPIWrapper");
        }

        /// <summary>
        /// This is the Partner API call for GetOrigin which gets the initial UI data
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public JObject GetOrigin(string transactionId)
        {
            JObject originData = null;

            try
            {
                // Get the OAuth Access Token
                var oAuthToken = GetPartnerOAuthToken();

                if (oAuthToken.TokenString != null)
                {
                    var partnerAPIRequestURI = _AppSettings.PartnerAPI.OriginURI.Replace("{{transactionId}}", transactionId);
                    var partnerAPIHeader = new Dictionary<string, string>
                    {
                        { "Authorization", "Bearer " + oAuthToken.TokenString }, 
                        { "Content-Type", "application/json" }
                    };

                    var response = HttpCommunicationHelper.ExecuteRequest(partnerAPIHeader, _AppSettings.PartnerAPI.EndPoint, partnerAPIRequestURI, RestSharp.Method.GET, "");

                    if (response != null && response.StatusCode == HttpStatusCode.OK)
                    {
                        originData = JObject.Parse(response.Content);
                    }

                    _Logger.LogInformation("[PartnerAPIWrapper] - Get Origin - Transaction Id - " + transactionId);
                    _Logger.LogInformation("[PartnerAPIWrapper] - Get Origin - Response - " + originData);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPIWrapper] - CreateRequest - Exception - " + ex.Message);
            }

            return originData;
        }

        /// <summary>
        /// This method will call the Create Request of Partner API
        /// </summary>
        /// <param name="uiData"></param>
        /// <param name="transactionId"></param>
        public void CreateRequest(string uiData, string transactionId)
        {
            try
            {
                _Logger.LogInformation("[PartnerAPIWrapper] - CreateRequest - Before Get OAuthToken - ");

                // Getting the OAuth Access Token
                var oAuthToken = GetPartnerOAuthToken();

                if (oAuthToken.TokenString != null)
                {
                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateRequest - OAuthToken is not null - ");

                    var partnerAPIRequestURI = _AppSettings.PartnerAPI.RequestURI.Replace("{{transactionId}}", transactionId);

                    var partnerAPIHeader = new Dictionary<string, string>
                    {
                        { "Authorization", "Bearer " + oAuthToken.TokenString },
                        { "Content-Type", "application/json" }
                    };

                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateRequest - Before Execute Request - ");

                    //// Adding transaction status in the cache (This will be partner specific). This cache will be polled by the UI to get a status of the current transaction
                    //TransactionCache.Instance.Add(transactionId, "pending");

                    var response = HttpCommunicationHelper.ExecuteRequest(partnerAPIHeader, _AppSettings.PartnerAPI.EndPoint, partnerAPIRequestURI, RestSharp.Method.POST, uiData);

                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateRequest - Post Data - " + uiData);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPIWrapper] - CreateRequest - Exception - " + ex.Message);
            }
        }

        /// <summary>
        /// This method will do the GetRequest call to the Partner API
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public JObject GetRequest(string transactionId)
        {
            JObject requestData = null;

            try
            {
                _Logger.LogInformation("[PartnerAPIWrapper] - GetRequest - Before Get OAuthToken - ");

                // Getting the OAuth Access Token
                var oAuthToken = GetPartnerOAuthToken();

                if (oAuthToken.TokenString != null)
                {
                    _Logger.LogInformation("[PartnerAPIWrapper] - GetRequest - OAuthToken is not null - ");

                    var partnerAPIRequestURI = _AppSettings.PartnerAPI.RequestURI.Replace("{{transactionId}}", transactionId);
                    var partnerAPIHeader = new Dictionary<string, string>
                    {
                        { "Authorization", "Bearer " + oAuthToken.TokenString },
                        { "Content-Type", "application/json" }
                    };

                    _Logger.LogInformation("[PartnerAPIWrapper] - GetRequest - Before Execute Request - ");

                    var response = HttpCommunicationHelper.ExecuteRequest(partnerAPIHeader, _AppSettings.PartnerAPI.EndPoint, partnerAPIRequestURI, RestSharp.Method.GET, "");

                    if (response != null && response.StatusCode == HttpStatusCode.OK)
                    {
                        requestData = new JObject();
                        requestData = JObject.Parse(response.Content);
                    }

                    _Logger.LogInformation("[PartnerAPIWrapper] - GetRequest - Request Data - " + requestData);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPIWrapper] - GetRequest - Exception - " + ex.Message);
            }

            return requestData;
        }

        /// <summary>
        /// This method will do a CreateResponse call to the Partner API
        /// </summary>
        /// <param name="responseData"></param>
        /// <param name="transactionId"></param>
        public void CreateResponse(string responseData, string transactionId)
        {
            try
            {
                _Logger.LogInformation("[PartnerAPIWrapper] - CreateResponse - Before Get OAuthToken - ");

                // Getting the OAuth Access Token
                var oAuthToken = GetPartnerOAuthToken();

                if (oAuthToken.TokenString != null)
                {
                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateResponse - OAuthToken is not null - ");

                    var partnerAPIRequestURI = _AppSettings.PartnerAPI.ResponseURI.Replace("{{transactionId}}", transactionId);
                    var partnerAPIHeader = new Dictionary<string, string>
                    {
                        { "Authorization", "Bearer " + oAuthToken.TokenString },
                        { "Content-Type", "application/json" }
                    };

                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateResponse - Before Execute Request - ");

                    //// Adding transaction status in the cache (This will be partner specific). This cache will be polled by the UI to get a status of the current transaction
                    //TransactionCache.Instance.Add(transactionId, "pending");
                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateResponse - Post Data - " + responseData);

                    var response = HttpCommunicationHelper.ExecuteRequest(partnerAPIHeader, _AppSettings.PartnerAPI.EndPoint, partnerAPIRequestURI, RestSharp.Method.POST, responseData);
                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateResponse - After ExecuteRequest");
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPIWrapper] - CreateResponse - Exception - " + ex.Message);
            }
        }

        /// <summary>
        /// This method will return a URL that the partner can use to post their attachments
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public string GetDropFilesURL(string transactionId)
        {
            var dropFilesURL = string.Empty;
            try
            {
                _Logger.LogInformation("[PartnerAPIWrapper] - GetDropFilesURL - Before Get OAuthToken - ");

                // Getting the OAuth Access Token
                var oAuthToken = GetPartnerOAuthToken();

                if (oAuthToken.TokenString != null)
                {
                    _Logger.LogInformation("[PartnerAPIWrapper] - GetDropFilesURL - OAuthToken is not null - ");

                    var partnerAPIRequestURI = _AppSettings.PartnerAPI.DropFilesURI.Replace("{{transactionId}}", transactionId);
                    var partnerAPIHeader = new Dictionary<string, string>
                    {
                        { "Authorization", "Bearer " + oAuthToken.TokenString },
                        { "Content-Type", "application/json" }
                    };

                    _Logger.LogInformation("[PartnerAPIWrapper] - GetDropFilesURL - Before Execute Request");

                    var response = HttpCommunicationHelper.ExecuteRequest(partnerAPIHeader, _AppSettings.PartnerAPI.EndPoint, partnerAPIRequestURI, RestSharp.Method.GET, "");

                    if (response != null && response.StatusCode == HttpStatusCode.Created)
                    {
                        var responseHeaders = response.Headers.ToList();
                        var attachmentURL = responseHeaders.Find(x => x.Name.ToLower() == "location") != null ? responseHeaders.Find(x => x.Name.ToLower() == "location").Value.ToString() : string.Empty;

                        dropFilesURL = attachmentURL;

                        _Logger.LogInformation("[PartnerAPIWrapper] - GetDropFilesURL - DropFilesURL - " +  dropFilesURL);
                    }

                    //_Logger.LogInformation("[PartnerAPIWrapper] - GetDropFilesURL - Request Data - " + requestData);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPIWrapper] - GetDropFilesURL - Exception - " + ex.Message);
            }

            return dropFilesURL;
        }

       /// <summary>
       /// This method will upload files to the Media Server
       /// </summary>
       /// <param name="uploadURL"></param>
       /// <param name="fileInfoList"></param>
       /// <returns></returns>
        public string UploadFilesToMediaServer(string uploadURL, IList<FileInfo> fileInfoList)
        {
            var uploadResponse = string.Empty;

            try
            {
                var partnerAPIRequestURI = uploadURL;
                var partnerAPIHeader = new Dictionary<string, string>
                {
                    { "Content-Type", "multipart/form-data" }
                };

                _Logger.LogInformation("[PartnerAPIWrapper] - UploadFilesToEFolder - Before Execute Request - ");
                var response = HttpCommunicationHelper.ExecuteRequest(partnerAPIHeader, partnerAPIRequestURI, RestSharp.Method.POST, fileInfoList);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    uploadResponse = response.Content;
                }

                _Logger.LogInformation("[PartnerAPIWrapper] - UploadFilesToEFolder - Response Content - " + response.Content);
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPIWrapper] - UploadFilesToEFolder - Exception - " + ex.Message);
            }

            return uploadResponse;
        }


        /// <summary>
        /// Gets the Partner OAuth Token required for making Partner API calls
        /// </summary>
        /// <returns></returns>
        private Token GetPartnerOAuthToken()
        {
            Token oAuthToken = null;

            try
            {
                var OAuthKeyDictionary = new Dictionary<string, string>();

                // Gets the headers required for generating the OAuth Access Token
                OAuthKeyDictionary = OAuthWrapper.GetOAuthHeader(_AppSettings);

                IOAuthWrapper oAuthWrapper = new OAuthWrapper(OAuthKeyDictionary, _AppSettings.APIHost, _AppSettings.OAuthTokenEndPoint);

                // Get the OAuth Access Token
                oAuthToken = oAuthWrapper.GetOAuthAccessToken();
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPIWrapper] - GetOAuthToken - Exception - " + ex.Message);
            }

            return oAuthToken;
        }

    }
}
