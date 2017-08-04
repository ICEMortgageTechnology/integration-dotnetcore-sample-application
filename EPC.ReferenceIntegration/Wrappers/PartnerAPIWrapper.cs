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
using Newtonsoft.Json;

namespace EPC.ReferenceIntegration.Wrappers
{
    /// <summary>
    /// This Class is a wrapper over the Partner API
    /// This Class includes the following Partner API's
    /// 1.  GetOrigin
    /// 2.  CreateRequest
    /// 3.  GetRequest
    /// 4.  DropFiles
    /// 5.  UploadFilesToMediaServer
    /// 6.  CreateResponse
    /// </summary>
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
                _Logger.LogInformation("[PartnerAPIWrapper] - Get Origin - Before Get OAuthToken - ");

                // Get the OAuth Access Token
                var oAuthToken = GetPartnerOAuthToken();

                if (oAuthToken.TokenString != null)
                {
                    // replacing transactionId in the URL
                    var partnerAPIRequestURI = _AppSettings.PartnerAPI.OriginURI.Replace("{{transactionId}}", transactionId);
                    var partnerAPIHeader = GetHeaderForPartnerAPI(oAuthToken.TokenString);

                    // Executing the Partner API Request
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
                _Logger.LogError("[PartnerAPIWrapper] - GetOrigin - Exception - " + ex.Message);
                _Logger.LogError("[PartnerAPIWrapper] - GetOrigin - StackTrace - " + ex.StackTrace);
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

                    // replacing transactionId in the URL
                    var partnerAPIRequestURI = _AppSettings.PartnerAPI.RequestURI.Replace("{{transactionId}}", transactionId);
                    var partnerAPIHeader = GetHeaderForPartnerAPI(oAuthToken.TokenString);

                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateRequest - Before Execute Request - ");

                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateRequest - Post Data - " + JObject.Parse(uiData).ToString(Formatting.Indented));

                    // Executing the Partner API Request
                    var response = HttpCommunicationHelper.ExecuteRequest(partnerAPIHeader, _AppSettings.PartnerAPI.EndPoint, partnerAPIRequestURI, RestSharp.Method.POST, uiData);                    
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPIWrapper] - CreateRequest - Exception - " + ex.Message);
                _Logger.LogError("[PartnerAPIWrapper] - CreateRequest - StackTrace - " + ex.StackTrace);
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

                    // replacing transactionId in the URL
                    var partnerAPIRequestURI = _AppSettings.PartnerAPI.RequestURI.Replace("{{transactionId}}", transactionId);
                    var partnerAPIHeader = GetHeaderForPartnerAPI(oAuthToken.TokenString);

                    _Logger.LogInformation("[PartnerAPIWrapper] - GetRequest - Before Execute Request - ");

                    // Executing the Partner API Request
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
                _Logger.LogError("[PartnerAPIWrapper] - GetRequest - StackTrace - " + ex.StackTrace);
            }

            return requestData;
        }

        /// <summary>
        /// This method will do a CreateResponse call to the Partner API
        /// </summary>
        /// <param name="responseData"></param>
        /// <param name="transactionId"></param>
        public bool CreateResponse(string responseData, string transactionId)
        {
            var isResponseCreated = false;

            try
            {
                _Logger.LogInformation("[PartnerAPIWrapper] - CreateResponse - Before Get OAuthToken - ");

                // Getting the OAuth Access Token
                var oAuthToken = GetPartnerOAuthToken();

                if (oAuthToken.TokenString != null)
                {
                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateResponse - OAuthToken is not null - ");

                    // replacing transactionId in the URL
                    var partnerAPIRequestURI = _AppSettings.PartnerAPI.ResponseURI.Replace("{{transactionId}}", transactionId);
                    var partnerAPIHeader = GetHeaderForPartnerAPI(oAuthToken.TokenString);

                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateResponse - Before Execute Request - ");
                    
                    // Adding the response in log
                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateResponse - Post Data - " + JObject.Parse(responseData).ToString(Formatting.Indented));

                    // Executing the Partner API Request
                    var response = HttpCommunicationHelper.ExecuteRequest(partnerAPIHeader, _AppSettings.PartnerAPI.EndPoint, partnerAPIRequestURI, RestSharp.Method.POST, responseData);
                    _Logger.LogInformation("[PartnerAPIWrapper] - CreateResponse - After ExecuteRequest");

                    if (response.StatusCode == HttpStatusCode.Created)
                        isResponseCreated = true;
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPIWrapper] - CreateResponse - Exception - " + ex.Message);
                _Logger.LogError("[PartnerAPIWrapper] - CreateResponse - StackTrace - " + ex.StackTrace);
            }

            return isResponseCreated;
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

                    // replacing transactionId in the URL
                    var partnerAPIRequestURI = _AppSettings.PartnerAPI.DropFilesURI.Replace("{{transactionId}}", transactionId);
                    var partnerAPIHeader = GetHeaderForPartnerAPI(oAuthToken.TokenString);

                    _Logger.LogInformation("[PartnerAPIWrapper] - GetDropFilesURL - Before Execute Request");

                    // Executing the Partner API Request
                    var response = HttpCommunicationHelper.ExecuteRequest(partnerAPIHeader, _AppSettings.PartnerAPI.EndPoint, partnerAPIRequestURI, RestSharp.Method.GET, "");

                    if (response != null && response.StatusCode == HttpStatusCode.Created)
                    {
                        var responseHeaders = response.Headers.ToList();
                        var attachmentURL = responseHeaders.Find(x => x.Name.ToLower() == "location") != null ? responseHeaders.Find(x => x.Name.ToLower() == "location").Value.ToString() : string.Empty;

                        dropFilesURL = attachmentURL;

                        _Logger.LogInformation("[PartnerAPIWrapper] - GetDropFilesURL - DropFilesURL - " + dropFilesURL);
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPIWrapper] - GetDropFilesURL - Exception - " + ex.Message);
                _Logger.LogError("[PartnerAPIWrapper] - GetDropFilesURL - StackTrace - " + ex.StackTrace);
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

                // Executing the Partner API Request
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
                _Logger.LogError("[PartnerAPIWrapper] - UploadFilesToEFolder - StackTrace - " + ex.StackTrace);
            }

            return uploadResponse;
        }

        #region " Private Methods "

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
                OAuthKeyDictionary = OAuthWrapper.GetOAuthHeaders(_AppSettings);

                IOAuthWrapper oAuthWrapper = new OAuthWrapper(OAuthKeyDictionary, _AppSettings.APIHost, _AppSettings.OAuthTokenEndPoint);

                // Get the OAuth Access Token
                oAuthToken = oAuthWrapper.GetOAuthAccessToken();
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPIWrapper] - GetOAuthToken - Exception - " + ex.Message);
                _Logger.LogError("[PartnerAPIWrapper] - GetOAuthToken - StackTrace - " + ex.StackTrace);
            }

            return oAuthToken;
        }

        /// <summary>
        /// This function will build and return the header required for Partner API requests
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetHeaderForPartnerAPI(string token)
        {
            var partnerAPIHeader = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + token },
                { "Content-Type", "application/json" }
            };

            return partnerAPIHeader;
        }

        #endregion
    }
}
