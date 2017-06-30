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
    public static class HttpCommunicationHelper
    {
        private static ILoggerFactory _Factory;
        private static ILogger _Logger;

        static HttpCommunicationHelper()
        {
            _Factory = LogHelper.LoggerFactory;
            _Logger = _Factory.CreateLogger("HttpCommunicationHelper");
        }

        /// Execute POST using RestSharp
        /// </summary>
        /// <param name="requestParameters"></param>
        /// <param name="baseURL"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string ExecuteRequest(IEnumerable<KeyValuePair<string, string>> requestParameters, string baseURL, string uri, Method requestType)
        {
            var responseString = string.Empty;
            var restClient = new RestClient(baseURL);
            var request = new RestRequest(uri, requestType);
            var response = new RestResponse();

            _Logger.LogInformation("[HttpCommunicationHelper] In Execute Request Method");
            _Logger.LogInformation("[HttpCommunicationHelper] Request Type is " + requestType.ToString());

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

            _Logger.LogInformation("[HttpCommunicationHelper] Response Content - " + response.Content);

            return restResponse;
        }

        /// <summary>
        /// This method will take body as a byte
        /// </summary>
        /// <param name="requestParameters"></param>
        /// <param name="uploadURL"></param>
        /// <param name="requestType"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static RestResponse ExecuteRequest(IEnumerable<KeyValuePair<string, string>> requestParameters, string uploadURL, Method requestType, IList<FileInfo> fileInfoList)
        {
            RestResponse restResponse = null;
            var restClient = new RestClient(uploadURL);
            var request = new RestRequest(requestType);
            var response = new RestResponse();

            _Logger.LogInformation("[HttpCommunicationHelper] In Execute Request Method");
            _Logger.LogInformation("[HttpCommunicationHelper] Request Type is " + requestType.ToString());
            
            // add body if it is not null
            if (fileInfoList != null)
            {
                foreach (var item in fileInfoList)
                {
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
