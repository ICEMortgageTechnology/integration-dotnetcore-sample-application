using EPC.ReferenceIntegration.Models;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Helpers
{
    /// <summary>
    /// This class is a Helper class for all the http communications done by the Reference Integration
    /// It is using RestSharp plugin to make REST calls
    /// </summary>
    public static class HttpCommunicationHelper
    {
        private static ILoggerFactory _Factory;
        private static ILogger _Logger;

        /// <summary>
        /// static constructor
        /// </summary>
        static HttpCommunicationHelper()
        {
            _Factory = LogHelper.LoggerFactory;
            _Logger = _Factory.CreateLogger("HttpCommunicationHelper");
        }

        /// <summary>
        /// Execute POST using RestSharp
        /// </summary>
        /// <param name="requestParameters">Headers required for the REST call</param>
        /// <param name="baseURL">The base URL of the REST API</param>
        /// <param name="uri">The URI of the REST API</param>
        /// <param name="requestType">RequestType here is the REST Verbs (GET, PUT, PATCH, POST, DELETE)</param>
        /// <returns></returns>
        public static string ExecuteRequest(IEnumerable<KeyValuePair<string, string>> requestParameters, string baseURL, string uri, Method requestType)
        {
            var responseString = string.Empty;
            var restClient = new RestClient(baseURL);
            var request = new RestRequest(uri, requestType);
            var response = new RestResponse();

            _Logger.LogInformation("[HttpCommunicationHelper] In Execute Request Method");
            _Logger.LogInformation("[HttpCommunicationHelper] Request Type is " + requestType.ToString());
            _Logger.LogInformation("[HttpCommunicationHelper] Request Base URL is " + baseURL);
            _Logger.LogInformation("[HttpCommunicationHelper] Request URI is " + uri);

            // add request headers if its not null
            if (requestParameters != null)
            {
                _Logger.LogInformation("[HttpCommunicationHelper] requestParameters are not null.");

                foreach (var item in requestParameters)
                {
                    request.AddParameter(item.Key, item.Value);
                    _Logger.LogInformation("[HttpCommunicationHelper] requestParameter - Key - " + item.Key + " | Value - " + item.Value);
                }
            }

            // this will run the task asynchronously
            Task.Run(async () =>
            {
                response = await GetResponseContentAsync(restClient, request) as RestResponse;
            }).Wait();

            // add the response string if the status code is ok
            if (response.StatusCode == HttpStatusCode.OK)
            {
                responseString = response.Content;
                _Logger.LogInformation("[HttpCommunicationHelper] Status code is 200");
                _Logger.LogInformation(response.Content);
            }
            else
            {
                _Logger.LogInformation("[HttpCommunicationHelper] Status code was not 200");
                _Logger.LogInformation(response.Content);
            }

            _Logger.LogInformation("[HttpCommunicationHelper] Response URI - " + response.ResponseUri);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Content Type - " + response.ContentType);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status - " + response.ResponseStatus);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status Code - " + response.StatusCode.ToString());
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status Description - " + response.StatusDescription);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Content - " + response.Content);

            return responseString;
        }

        /// <summary>
        /// Execute Request
        /// </summary>
        /// <param name="requestParameters"></param>
        /// <param name="baseURL"></param>
        /// <param name="uri"></param>
        /// <param name="requestType"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static RestResponse ExecuteRequest(IEnumerable<KeyValuePair<string, string>> requestParameters, string baseURL, string uri, Method requestType, string body = "")
        {
            RestResponse restResponse = null;
            var restClient = new RestClient(baseURL);
            var request = new RestRequest(uri, requestType);
            var response = new RestResponse();

            _Logger.LogInformation("[HttpCommunicationHelper] In Execute Request Method");
            _Logger.LogInformation("[HttpCommunicationHelper] Request Type is " + requestType.ToString());
            _Logger.LogInformation("[HttpCommunicationHelper] Request Base URL is " + baseURL);
            _Logger.LogInformation("[HttpCommunicationHelper] Request URI is " + uri);

            // add body if it is not empty
            if (!string.IsNullOrEmpty(body))
            {
                _Logger.LogInformation("[HttpCommunicationHelper] Body is not null");

                var postdata = SimpleJson.DeserializeObject(body);
                request.AddJsonBody(postdata);

                _Logger.LogInformation("[HttpCommunicationHelper] Request Body - " + body);
            }

            // add request headers if its not null
            if (requestParameters != null)
            {
                _Logger.LogInformation("[HttpCommunicationHelper] requestParameters are not null.");

                foreach (var item in requestParameters)
                {
                    request.AddParameter(item.Key, item.Value, ParameterType.HttpHeader);
                    _Logger.LogInformation("[HttpCommunicationHelper] requestParameter - Key - " + item.Key + " | Value - " + item.Value);
                }
            }

            // this will run the task asynchronously
            Task.Run(async () =>
            {
                response = await GetResponseContentAsync(restClient, request) as RestResponse;
            }).Wait();

            restResponse = response;

            _Logger.LogInformation("[HttpCommunicationHelper] Response URI - " + response.ResponseUri);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Content Type - " + response.ContentType);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status - " + response.ResponseStatus);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status Code - " + response.StatusCode.ToString());
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status Description - " + response.StatusDescription);            
            _Logger.LogInformation("[HttpCommunicationHelper] Response Content - " + response.Content);

            return restResponse;
        }

        /// <summary>
        /// This method will take FileInfo list to execute the request
        /// </summary>
        /// <param name="requestParameters"></param>
        /// <param name="uploadURL"></param>
        /// <param name="requestType"></param>
        /// <param name="fileInfoList"></param>
        /// <returns></returns>
        public static RestResponse ExecuteRequest(IEnumerable<KeyValuePair<string, string>> requestParameters, string uploadURL, Method requestType, IList<FileInfo> fileInfoList)
        {
            RestResponse restResponse = null;
            var restClient = new RestClient(uploadURL);
            var request = new RestRequest(requestType);
            var response = new RestResponse();

            _Logger.LogInformation("[HttpCommunicationHelper] In Execute Request Method");
            _Logger.LogInformation("[HttpCommunicationHelper] Request Type is " + requestType.ToString());
            _Logger.LogInformation("[HttpCommunicationHelper] Request Base URL is " + uploadURL);

            // add body if it is not null
            if (fileInfoList != null)
            {
                foreach (var item in fileInfoList)
                {
                    _Logger.LogInformation("[HttpCommunicationHelper] Upload FileName " + item.FileName);
                    _Logger.LogInformation("[HttpCommunicationHelper] Upload ContentType " + item.ContentType);
                    _Logger.LogInformation("[HttpCommunicationHelper] Upload FileExtension " + item.FileExtension);                    

                    var filePath = string.Format(@"{0}\{1}.{2}", item.FilePath, item.FileName, item.FileExtension);
                    request.AddFile(item.Name, filePath, item.ContentType);                    
                }
            }

            request.AlwaysMultipartFormData = true;

            // add request headers if its not null
            if (requestParameters != null)
            {
                _Logger.LogInformation("[HttpCommunicationHelper] requestParameters are not null.");

                foreach (var item in requestParameters)
                {
                    request.AddParameter(item.Key, item.Value, ParameterType.HttpHeader);
                    _Logger.LogInformation("[HttpCommunicationHelper] requestParameter - Key - " + item.Key + " | Value - " + item.Value);
                }
            }

            // this will run the task asynchronously
            Task.Run(async () =>
            {
                response = await GetResponseContentAsync(restClient, request) as RestResponse;
            }).Wait();

            restResponse = response;

            _Logger.LogInformation("[HttpCommunicationHelper] Response URI - " + response.ResponseUri);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Content Type - " + response.ContentType);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status - " + response.ResponseStatus);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status Code - " + response.StatusCode.ToString());
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status Description - " + response.StatusDescription);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Content - " + response.Content);

            return restResponse;
        }

        /// <summary>
        /// This method will get execute the request and get the response asynchronously
        /// </summary>
        /// <param name="theClient"></param>
        /// <param name="theRequest"></param>
        /// <returns></returns>
        public static Task<IRestResponse> GetResponseContentAsync(RestClient theClient, RestRequest theRequest)
        {
            var tcs = new TaskCompletionSource<IRestResponse>();
            theClient.ExecuteAsync(theRequest, response => {
                tcs.SetResult(response);
            });
            return tcs.Task;
        }
    }    
}
