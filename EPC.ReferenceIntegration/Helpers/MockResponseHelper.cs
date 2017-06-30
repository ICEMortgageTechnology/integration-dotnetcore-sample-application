using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace EPC.ReferenceIntegration.Helpers
{
    public class MockResponseHelper
    {
        /// <summary>
        /// Thsi method result the UI date in Json Formate to teh caller
        /// </summary>
        /// <returns>JObject</returns>
        public static JObject GetUIDataResponse()
        {
            var mockFilePath = System.IO.Path.GetFullPath(System.IO.Directory.GetCurrentDirectory() + @"\ResponsePayloads\GetUIDataResponse.json");
            var jsonString = System.IO.File.ReadAllText(mockFilePath);
            JObject mockJsonObject = null;

            if (String.IsNullOrEmpty(jsonString))
            {
                Console.Write("Empty JSON Payload");
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
        
        /// <summary>
        /// This method will validate the loan data.
        /// basically the Partner will validate data coming back from the GetRequest call with their business rules and return a success or an error
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static JObject ValidateLoanData(JObject data)
        {
            JObject validationInfo = new JObject();

            if(data != null)
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

        #region "Building Mock Response Payloads for Partner API "

        /// <summary>
        /// This method will return a Mock object of VALoanData
        /// </summary>
        /// <returns></returns>
        public static JObject GetVALoanData()
        {
            // building VaLoanData object here
            dynamic vaLoanData = new JObject();
            vaLoanData.PropertyOccupancyType = "PropertyOccupancyType";
            vaLoanData.LotDimensions = "LotDimensions";
            vaLoanData.IrregularLotSizeInSquareFeet = "IrregularLotSizeInSquareFeet";
            vaLoanData.BuildingType = "BuildingType";
            vaLoanData.AppraisalType = "AppraisalType";

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
            underWritterSummary.AppraisalOrderedDate = "Appraisal Ordered Date";
            underWritterSummary.AppraisalCompletionDate = "Appraisal Completion Date";
            underWritterSummary.Appraisal = "Appraisal";
            underWritterSummary.AppraisalType = "Some Appraisal";
            underWritterSummary.OriginalAppraiser = "Original Appraiser";
            underWritterSummary.OriginalAppraisersValue = "OriginalAppraisersValue";

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
            uidd.PropertyValuationEffectiveDate = "PropertyValuationEffectiveDate";

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
            tsum.LevelOfPropertyReviewType = "LevelOfPropertyReviewType";

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
            property.StructureBuiltYear = 2016;
            property.NumberOfStories = 3;
            property.FinancedNumberOfUnits = 2;
            property.BuildingStatusType = "BuildingStatusType";

            property.AppraisedAmount = 50000;
            property.AppraisedValueAmount = 50000;

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
            hudLoanData.PropertyType = "PropertyType";

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
            hmda.PropertyType = "PropertyType";

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

            fees.FeeType = "AppraisalFee";
            fees.BorPaidAmount = 500;
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
            floodContact.InsuranceNoOfBedrooms = 5;
            floodContact.InsuranceFloodZone = "InsuranceFloodZone";
            loanContacts.Add(floodContact);

            dynamic appraisalContact = new JObject();
            appraisalContact.ContactType = "APPRAISAL_COMPANY";
            appraisalContact.ReferenceNumber = "ReferenceNumber";
            appraisalContact.Phone = "Phone";
            appraisalContact.PersonalLicenseNumber = "PersonalLicenseNumber";
            appraisalContact.Fax = "Fax";
            appraisalContact.ContactName = "ContactName";
            appraisalContact.Address = "Address";
            appraisalContact.City = "City";
            appraisalContact.State = "State";
            appraisalContact.PostalCode = "PostalCode";
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
            commitmentTerms.EstimatedRemainingYears = "EstimatedRemainingYears";

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
