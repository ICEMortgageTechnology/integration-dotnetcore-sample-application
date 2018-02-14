using EPC.ReferenceIntegration.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Wrappers.Interfaces
{
    public interface IPartnerAPI
    {
        /// <summary>
        /// This is the Partner API call for GetOrigin which gets the initial UI data
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        JObject GetOrigin(string transactionId);

        /// <summary>
        /// This method will call the Create Request of Partner API
        /// </summary>
        /// <param name="uiData"></param>
        /// <param name="transactionId"></param>
        void CreateRequest(string uiData, string transactionId);

        void UpdateRequest(string uiData, string transactionId);

        /// <summary>
        /// This method will do the GetRequest call to the Partner API
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        JObject GetRequest(string transactionId);

        /// <summary>
        /// This method will do a CreateResponse call to the Partner API
        /// </summary>
        /// <param name="responseData"></param>
        /// <param name="transactionId"></param>
        bool CreateResponse(string responseData, string transactionId);

        /// <summary>
        /// This method will return a URL that the partner can use to post their attachments
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        string GetDropFilesURL(string transactionId);

        /// <summary>
        /// This method will upload files to the Media Server
        /// </summary>
        /// <param name="uploadURL"></param>
        /// <param name="fileInfoList"></param>
        /// <returns></returns>
        string UploadFilesToMediaServer(string uploadURL, IList<FileInfo> fileInfoList);
    }
}
