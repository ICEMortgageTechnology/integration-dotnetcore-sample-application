using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using EPC.ReferenceIntegration.DataStore;
using EPC.ReferenceIntegration.Wrappers;
using Microsoft.Extensions.Options;
using EPC.ReferenceIntegration.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using EPC.ReferenceIntegration.Models;

namespace EPC.ReferenceIntegration.Controllers
{
    [Produces("application/json")]
    [Route("api/Response")]
    public class ResponseController : Controller
    {
        private readonly AppSettings _AppSettings;
        private ILoggerFactory _Factory;
        private ILogger _Logger;
        private string _ClassName = string.Empty;

        public ResponseController(IOptions<AppSettings> appSettings)
        {
            _AppSettings = appSettings.Value;
            this._Factory = LogHelper.LoggerFactory;
            this._Logger = this._Factory.CreateLogger("ResponseController");
            this._ClassName = this.GetType().Name;
        }

        // POST: api/Response
        [HttpPost]
        [Route("{transactionId}")]
        public void Post([FromBody]JObject value, string transactionId)
        {
            // Submit Response to Partner API
            SubmitResponseToPartnerAPI(value, transactionId);
        }

        // PUT: api/Response/5
        [HttpPut("{orderId}")]
        public void Put(string orderId, [FromBody]string value)
        {
        }

        #region " Private Methods for Handing Submit Response "

        /// <summary>
        /// This method will be called when the order is fulfilled on the partners end.
        /// it will do a POST Response to Partner API
        /// </summary>
        /// <param name="response"></param>
        /// <param name="transactionId"></param>
        private void SubmitResponseToPartnerAPI(JObject response, string transactionId)
        {
            try
            {
                _Logger.LogInformation("[ResponseController] - SubmitResponseToPartnerAPI - Inside Method - ");

                // Getting the relevant Loan Information for the given Transaction
                var transactionLoanInformation = TransactionInformationCache.Instance.GetValue(transactionId);

                if (response != null)
                {
                    _Logger.LogInformation("[ResponseController] - SubmitResponseToPartnerAPI - response is not null - ");

                    if (transactionLoanInformation != null && response.SelectToken("status").ToString() == "Completed" && response.SelectToken("$.result") != null)
                    {
                        _Logger.LogInformation("[ResponseController] - SubmitResponseToPartnerAPI - TransactionLoanInformation is not null - ");

                        var result = response.SelectToken("$.result");

                        // build payload for Partner API Create Response
                        dynamic responsePayload = new JObject();

                        dynamic loanObject = new JObject();

                        loanObject.VaLoanData = MockResponseHelper.GetVALoanData();
                        loanObject.UnderwriterSummary = MockResponseHelper.GetUnderWritterSummaryData();
                        loanObject.Uldd = MockResponseHelper.GetUIDData();
                        loanObject.Tsum = MockResponseHelper.GetTSumData();
                        loanObject.Property = MockResponseHelper.GetPropertyData();
                        loanObject.HudLoanData = MockResponseHelper.GetHUDLoanData();
                        loanObject.Hmda = MockResponseHelper.GetHMDAData();
                        loanObject.Fees = MockResponseHelper.GetLoanFeesData();
                        loanObject.Contacts = MockResponseHelper.GetLoanContactsData();
                        loanObject.CommitmentTerms = MockResponseHelper.GetCommitmentTermsData();
                        loanObject.ClosingDocument = MockResponseHelper.GetClosingDocumentData();

                        dynamic orders = new JArray();
                        dynamic order = new JObject();

                        order.id = result["trackingId"];
                        order.orderDateTime = result["orderDate"];
                        order.orderStatus = response["status"];
                        order.orderMessage = result["orderMessage"];
                        order.product = transactionLoanInformation.ProductName;
                        order.documents = GetDocumentsFromResponse(response, transactionId);

                        orders.Add(order);

                        responsePayload.LoanData = loanObject;
                        responsePayload.orders = orders;

                        var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);
                        partnerAPIWrapper.CreateResponse(responsePayload.ToString(Formatting.None), transactionId);

                        _Logger.LogInformation("[ResponseController] - SubmitResponseToPartnerAPI - before adding response to TransactionStatus cache - ");
                        TransactionStatusCache.Instance.Add(transactionId, response);
                        _Logger.LogInformation("[ResponseController] - SubmitResponseToPartnerAPI - After adding response to TransactionStatus cache - ");
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[ResponseController] - SubmitResponseToPartnerAPI - Exception - " + ex.Message);
            }
        }

        /// <summary>
        /// Returns the documents received in the response JSON
        /// </summary>
        /// <param name="orderResponse"></param>
        /// <returns></returns>
        private JArray GetDocumentsFromResponse(JObject orderResponse, string transactionId)
        {
            dynamic documentsResponse = new JArray();

            try
            {
                if (orderResponse != null)
                {
                    _Logger.LogInformation("[ResponseController] - GetDocumentsFromResponse - Order response is not null ");

                    var embeddedFiles = orderResponse.SelectToken("$.result.orderResponse.REPORT.EMBEDDED_FILES");

                    if (embeddedFiles != null && embeddedFiles.HasValues)
                    {
                        _Logger.LogInformation("[ResponseController] - GetDocumentsFromResponse - Order response has attachments and EmbeddedFiles is not null ");

                        var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);
                        var uploadURL = partnerAPIWrapper.GetDropFilesURL(transactionId);

                        _Logger.LogInformation("[ResponseController] - GetDocumentsFromResponse - MediaServer URL is " + uploadURL);

                        if (!string.IsNullOrEmpty(uploadURL))
                        {
                            var fileInfoList = new List<FileInfo>();

                            var directoryName = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");

                            // Populating file info list for uploading to the Media server
                            foreach (var item in embeddedFiles.Children())
                            {
                                var file = item["DOCUMENT"];

                                var fileInfo = new FileInfo
                                {
                                    Name = item["_Name"].ToString(),
                                    FileName = item["_Name"].ToString(),
                                    FileExtension = item["_Type"].ToString(),
                                    ContentType = item["MIMEType"].ToString(),
                                    Content = Convert.FromBase64String(file.ToString()),
                                    FileDirectory = directoryName,
                                    FilePath = CreateAndGetFilePath(directoryName, file.ToString(), item["_Name"].ToString(), item["_Type"].ToString())
                                };

                                fileInfoList.Add(fileInfo);
                            }

                            var mediaServerResponse = UploadFilesToMediaServer(uploadURL, fileInfoList);

                            if (mediaServerResponse != null)
                            {
                                _Logger.LogInformation("[ResponseController] - GetDocumentsFromResponse - MediaServer response is not null ");

                                dynamic document = new JObject();
                                document.name = "Credit Report";
                                document.attachments = mediaServerResponse;
                                documentsResponse.Add(document);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[ResponseController] - GetDocumentsFromResponse - Exception - " + ex.Message);
            }

            return documentsResponse;
        }

        /// <summary>
        /// This method will upload files to the media server and return a list of attachments
        /// </summary>
        /// <param name="mediaServerURL"></param>
        /// <param name="fileInfoList"></param>
        /// <returns></returns>
        private JArray UploadFilesToMediaServer(string mediaServerURL, IList<FileInfo> fileInfoList)
        {
            dynamic attachmentsList = new JArray();

            try
            {
                if (!string.IsNullOrEmpty(mediaServerURL) && fileInfoList != null)
                {
                    var partnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings);
                    var response = partnerAPIWrapper.UploadFilesToMediaServer(mediaServerURL, fileInfoList);

                    if (!string.IsNullOrEmpty(response))
                    {
                        _Logger.LogInformation("[ResponseController] - UploadFilesToMediaServer - File Upload was successful ");

                        var attachmentsResponse = JObject.Parse(response);

                        if (attachmentsResponse != null && attachmentsResponse.SelectToken("Files") != null)
                        {
                            var files = attachmentsResponse.SelectToken("Files");

                            dynamic attachments = new JArray();

                            foreach (var item in files.Children())
                            {
                                foreach (var file in fileInfoList)
                                {
                                    var fileName = string.Format(@"{0}.{1}", file.Name, file.FileExtension);

                                    if (item["FileName"].ToString().ToLower().Contains(fileName.ToLower()))
                                    {
                                        dynamic attachment = new JObject();

                                        attachment.id = item["FileId"];
                                        attachment.name = fileName;
                                        attachment.mimeType = file.ContentType;
                                        attachments.Add(attachment);

                                        // delete the temporary file from local system
                                        if (System.IO.Directory.Exists(file.FilePath))
                                            System.IO.Directory.Delete(file.FilePath, true);

                                        _Logger.LogInformation("[ResponseController] - UploadFilesToMediaServer - AttachmentID - " + item["FileId"].ToString());

                                        break;
                                    }
                                }
                            }

                            attachmentsList = attachments;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[ResponseController] - UploadFilesToMediaServer - Exception - " + ex.Message);
            }

            return attachmentsList;
        }

        /// <summary>
        /// This method will create a file and get the file path 
        /// It is basically used to create a temporary file for Upload
        /// </summary>
        /// <param name="directoryName"></param>
        /// <param name="fileContent"></param>
        /// <param name="name"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        private string CreateAndGetFilePath(string directoryName, string fileContent, string name, string extension)
        {
            var fileName = string.Format("{0}.{1}", name, extension);

            var directoryPath = System.IO.Path.GetFullPath(System.IO.Directory.GetCurrentDirectory() + @"\Temp\" + directoryName);
            var filePath = string.Format(@"{0}\{1}", directoryPath, fileName);

            // create the temporary directory if it does not exist.
            if (!System.IO.Directory.Exists(directoryPath))
                System.IO.Directory.CreateDirectory(directoryPath);

            // create the temporary file if it does not exist.
            if (!System.IO.File.Exists(filePath))
                System.IO.File.WriteAllBytes(filePath, Convert.FromBase64String(fileContent));

            return directoryPath;
        }

        #endregion
        
    }
}
