using EPC.ReferenceIntegration.DataStore;
using EPC.ReferenceIntegration.Models;
using EPC.ReferenceIntegration.Wrappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace EPC.ReferenceIntegration.Helpers
{
    /// <summary>
    /// This class is a Helper class to generate Mock responses for Partner API calls
    /// </summary>
    public class MockResponseHelper
    {
        private static ILogger _Logger;
        private static ILoggerFactory _Factory;
        private static string _ClassName = string.Empty;
        static MockResponseHelper()
        {
            _Factory = LogHelper.LoggerFactory;
            _Logger = _Factory.CreateLogger("MockResponseHelper");
            _ClassName = "MockResponseHelper";
        }

        /// <summary>
        /// This method result the UI date in Json Formate to teh caller
        /// </summary>
        /// <returns>JObject</returns>
        public static JObject GetUIDataResponse()
        {
            var mockFilePath = System.IO.Path.GetFullPath(System.IO.Directory.GetCurrentDirectory() + @"\ResponsePayloads\GetUIDataResponse.json");
            var jsonString = System.IO.File.ReadAllText(mockFilePath);
            JObject mockJsonObject = null;

            if (String.IsNullOrEmpty(jsonString))
            {
                _Logger.LogInformation("[MockResponseHelper] - GetUIDataResponse - UIDataResponse is empty");
            }
            else
            {
                try
                {
                    mockJsonObject = JObject.Parse(jsonString);
                }
                catch (JsonReaderException ex)
                {
                    _Logger.LogError("[MockResponseHelper] - GetUIDataResponse - Response is not a valid JSON");
                    _Logger.LogError("[MockResponseHelper] - GetUIDataResponse - Exception - " + ex.Message);
                }
                catch (Exception ex)
                {
                    _Logger.LogError("[MockResponseHelper] - GetUIDataResponse - Exception - " + ex.Message);
                }
            }
            return mockJsonObject;
        }

        /// <summary>
        /// This method will validate the loan data.
        /// basically the Partner will validate data coming back from the GetRequest call with their business rules and return a success or an error
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static JObject ValidateLoanData(JObject data)
        {
            JObject validationInfo = new JObject();

            if (data != null)
            {
                // Implement partner specific validation here

                var mockSuccessResponsePath = System.IO.Path.GetFullPath(System.IO.Directory.GetCurrentDirectory() + @"\ResponsePayloads\PartnerValidationSuccess.json");
                var successResponse = System.IO.File.ReadAllText(mockSuccessResponsePath);

                validationInfo.Add("success", JToken.FromObject(successResponse));
            }
            else
            {
                var mockErrorResponsePath = System.IO.Path.GetFullPath(System.IO.Directory.GetCurrentDirectory() + @"\ResponsePayloads\PartnerValidationError.json");
                var errorResponse = System.IO.File.ReadAllText(mockErrorResponsePath);

                validationInfo.Add("error", JToken.FromObject(errorResponse));
            }

            return validationInfo;
        }

        /// <summary>
        /// This method will return the Mock response for Check status
        /// </summary>
        /// <returns></returns>
        public static JObject GetResponseForCheckStatus(string transactionId, string orderId, AppSettings appSettings)
        {
            var mockFilePath = System.IO.Path.GetFullPath(System.IO.Directory.GetCurrentDirectory() + @"\ResponsePayloads\PartnerOrderFulFilledResponse.json");
            var jsonString = System.IO.File.ReadAllText(mockFilePath);
            JObject mockJsonObject = null;

            if (!String.IsNullOrEmpty(jsonString))
            {
                // Getting the relevant Loan Information for the given Transaction
                var transactionLoanInformation = TransactionInformationCache.Instance.GetValue(orderId);

                var response = JObject.Parse(jsonString);

                if (response != null)
                {
                    _Logger.LogInformation("[MockResponseHelper] - SubmitResponseToPartnerAPI - response is not null - ");

                    if (transactionLoanInformation != null && response.SelectToken("status").ToString() == "Completed" && response.SelectToken("$.result") != null)
                    {
                        _Logger.LogInformation("[MockResponseHelper] - SubmitResponseToPartnerAPI - TransactionLoanInformation is not null - ");

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
                        loanObject.PropertyAppraisedValueAmount = 566000;

                        dynamic orders = new JArray();
                        dynamic order = new JObject();

                        //var orderCompletionDate = DateTime.Now.ToUniversalTime();
                        var orderCompletionDate = DateTime.Now;

                        order.id = orderId;
                        order.orderDateTime = orderCompletionDate.ToString("yyyy-MM-ddTHH:mm-ss:ff");
                        order.orderStatus = response["status"];
                        order.orderMessage = result["orderMessage"];
                        order.product = transactionLoanInformation.ProductName;
                        order.documents = GetDocumentsFromResponse(response, transactionId, appSettings);

                        orders.Add(order);

                        responsePayload.loanData = loanObject;
                        responsePayload.orders = orders;

                        mockJsonObject = responsePayload;
                    }
                }
            }
            else
            {
                try
                {
                    mockJsonObject = JObject.Parse(jsonString);
                }
                catch (JsonReaderException ex)
                {
                    Console.Write("Not a valid JSON paylod : " + ex);
                }
                catch (Exception ex)
                {
                    Console.Write("Exception occuerd : " + ex);
                }
            }
            return mockJsonObject;
        }

        #region " Mock Order Fulfilled Response "

        /// <summary>
        /// Returns the documents received in the response JSON
        /// </summary>
        /// <param name="orderResponse"></param>
        /// <param name="transactionId"></param>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        private static JArray GetDocumentsFromResponse(JObject orderResponse, string transactionId, AppSettings appSettings)
        {
            dynamic documentsResponse = new JArray();

            try
            {
                if (orderResponse != null)
                {
                    _Logger.LogInformation("[MockResponseHelper] - GetDocumentsFromResponse - Order response is not null ");

                    var embeddedFiles = orderResponse.SelectToken("$.result.orderResponse.REPORT.EMBEDDED_FILES");

                    if (embeddedFiles != null && embeddedFiles.HasValues)
                    {
                        _Logger.LogInformation("[MockResponseHelper] - GetDocumentsFromResponse - Order response has attachments and EmbeddedFiles is not null ");

                        var partnerAPIWrapper = new PartnerAPIWrapper(appSettings);
                        var uploadURL = partnerAPIWrapper.GetDropFilesURL(transactionId);

                        _Logger.LogInformation("[MockResponseHelper] - GetDocumentsFromResponse - MediaServer URL is " + uploadURL);

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

                            var mediaServerResponse = UploadFilesToMediaServer(uploadURL, fileInfoList, appSettings);

                            if (mediaServerResponse != null)
                            {
                                _Logger.LogInformation("[MockResponseHelper] - GetDocumentsFromResponse - MediaServer response is not null ");

                                dynamic document = new JObject();
                                document.name = "Appraisal Report";
                                document.attachments = mediaServerResponse;
                                documentsResponse.Add(document);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[MockResponseHelper] - GetDocumentsFromResponse - Exception - " + ex.Message);
            }

            return documentsResponse;
        }

        /// <summary>
        /// This method will upload files to the media server and return a list of attachments
        /// </summary>
        /// <param name="mediaServerURL"></param>
        /// <param name="fileInfoList"></param>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        private static JArray UploadFilesToMediaServer(string mediaServerURL, IList<FileInfo> fileInfoList, AppSettings appSettings)
        {
            dynamic attachmentsList = new JArray();

            try
            {
                if (!string.IsNullOrEmpty(mediaServerURL) && fileInfoList != null)
                {
                    var partnerAPIWrapper = new PartnerAPIWrapper(appSettings);
                    var response = partnerAPIWrapper.UploadFilesToMediaServer(mediaServerURL, fileInfoList);

                    if (!string.IsNullOrEmpty(response))
                    {
                        _Logger.LogInformation("[MockResponseHelper] - UploadFilesToMediaServer - File Upload was successful ");

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

                                        _Logger.LogInformation("[MockResponseHelper] - UploadFilesToMediaServer - AttachmentID - " + item["FileId"].ToString());

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
                _Logger.LogError("[MockResponseHelper] - UploadFilesToMediaServer - Exception - " + ex.Message);
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
        private static string CreateAndGetFilePath(string directoryName, string fileContent, string name, string extension)
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

        #region "Building Mock Response Payloads for Partner API "

        /// <summary>
        /// This method will return a Mock object of VALoanData
        /// </summary>
        /// <returns></returns>
        public static JObject GetVALoanData()
        {
            // building VaLoanData object here
            dynamic vaLoanData = new JObject();
            vaLoanData.PropertyOccupancyType = "Occupied By owner";
            vaLoanData.LotDimensions = 5600;
            vaLoanData.IrregularLotSizeInSquareFeet = 5800;
            vaLoanData.BuildingType = "Detached";
            vaLoanData.AppraisalType = "MCRV";
            vaLoanData.StreetAccess = "Some Street";
            vaLoanData.StreetMaintenance = "Some Street Maintenance";

            return vaLoanData;
        }

        /// <summary>
        /// This method will return a Mock object of GetUnderWritterSummaryData
        /// </summary>
        /// <returns></returns>
        public static JObject GetUnderWritterSummaryData()
        {
            // building UnderwritterSummary object here
            dynamic underWritterSummary = new JObject();
            underWritterSummary.AppraisalOrderedDate = "2017-07-15T00:00:00Z";
            underWritterSummary.AppraisalCompletedDate = "2017-07-26T00:00:00Z";
            underWritterSummary.Appraisal = "Appraisal";
            underWritterSummary.AppraisalType = "MCRV";
            underWritterSummary.OriginalAppraiser = "Original Appraiser";
            underWritterSummary.OriginalAppraisersValue = 16000;

            return underWritterSummary;
        }

        /// <summary>
        /// This method will return a Mock object of GetUIDData
        /// </summary>
        /// <returns></returns>
        public static JObject GetUIDData()
        {
            // building Uidd object here
            dynamic uidd = new JObject();
            uidd.AppraisalIdentifier = "Some Appraisal Identifier";
            uidd.PropertyValuationEffectiveDate = "2017-05-02T00:00:00Z";

            return uidd;
        }

        /// <summary>
        /// This method will return a Mock object of GetTSumData
        /// </summary>
        /// <returns></returns>
        public static JObject GetTSumData()
        {
            // building Tsum (Property Review) object here
            dynamic tsum = new JObject();
            tsum.LevelOfPropertyReviewType = "Exterior Only";

            return tsum;
        }

        /// <summary>
        /// This method will return a Mock object of GetPropertyData
        /// </summary>
        /// <returns></returns>
        public static JObject GetPropertyData()
        {
            // building property object here
            dynamic property = new JObject();

            property.StructureBuiltYear = 1962;
            property.NumberOfStories = 10;
            property.FinancedNumberOfUnits = 5;
            property.BuildingStatusType = "Existing";
            property.AppraisedAmount = 555000;
            property.AppraisedValueAmount = 666000;

            return property;
        }

        /// <summary>
        /// This method will return a Mock object of GetHUDLoanData
        /// </summary>
        /// <returns></returns>
        public static JObject GetHUDLoanData()
        {
            // building HudLoanData object here
            dynamic hudLoanData = new JObject();
            hudLoanData.PropertyType = "Condominium";

            return hudLoanData;
        }

        /// <summary>
        /// This method will return a Mock object of GetHMDAData
        /// </summary>
        /// <returns></returns>
        public static JObject GetHMDAData()
        {
            // building HMDA object here
            dynamic hmda = new JObject();
            hmda.PropertyType = "Manufactured Housing";

            return hmda;
        }

        /// <summary>
        /// This method will return a Mock object of GetLoanFeesData
        /// </summary>
        /// <returns></returns>
        public static JArray GetLoanFeesData()
        {
            // building loan fees object here
            var loanFees = new JArray();
            dynamic fees = new JObject();

            fees.FeeType = "CommissionPaid";
            fees.BorPaidAmount = 9850;
            loanFees.Add(fees);

            return loanFees;
        }

        /// <summary>
        /// This method will return a Mock object of GetLoanContactsData
        /// </summary>
        /// <returns></returns>
        public static JArray GetLoanContactsData()
        {
            // building loan contacts object here
            var loanContacts = new JArray();

            dynamic floodContact = new JObject();
            floodContact.ContactType = "FLOOD_INSURANCE";
            floodContact.InsuranceNoOfBedrooms = 15;
            floodContact.InsuranceFloodZone = false;
            loanContacts.Add(floodContact);

            dynamic appraisalContact = new JObject();
            appraisalContact.ContactType = "APPRAISAL_COMPANY";
            appraisalContact.ReferenceNumber = "25D";
            appraisalContact.Phone = "256-589-5897";
            appraisalContact.PersonalLicenseNumber = "H224485";
            appraisalContact.Fax = "256-859-6574";
            appraisalContact.ContactName = "Test Appraisal Contact";
            appraisalContact.Address = "108 ASCOT DRIVE. SOUTHLAKE, TX 76092";
            appraisalContact.City = "SOUTHLAKE";
            appraisalContact.State = "TX";
            appraisalContact.PostalCode = "76092";
            loanContacts.Add(appraisalContact);

            return loanContacts;
        }

        /// <summary>
        /// This method will return a Mock object of GetCommitmentTermsData
        /// </summary>
        /// <returns></returns>
        public static JObject GetCommitmentTermsData()
        {
            // building CommitmentTerms object here
            dynamic commitmentTerms = new JObject();
            commitmentTerms.EstimatedRemainingYears = 25;

            return commitmentTerms;
        }

        /// <summary>
        /// This method will return a Mock object of GetClosingDocumentData
        /// </summary>
        /// <returns></returns>
        public static JObject GetClosingDocumentData()
        {
            // building Closing Document object here
            dynamic closingDocument = new JObject();
            closingDocument.SpecialFloodHazardAreaIndictor = "SpecialFloodHazardAreaIndictor";

            return closingDocument;
        }

        #endregion
        
    }
}
