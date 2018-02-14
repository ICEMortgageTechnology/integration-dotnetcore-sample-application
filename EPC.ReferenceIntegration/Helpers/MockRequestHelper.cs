using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Helpers
{
    public class MockRequestHelper
    {
        private AppSettings _AppSettings = null;
        private string _IntegrationType = "Appraisal"; // By default our Integration Type would be Appraisal meaning the Reference Integration will always return responses for Appraisal Type.
        private JObject _PartnerProduct;
        private string jsonProdPath = "$.products[?(@.code == '{0}')]";

        /// <summary>
        /// Public Constructor which takes AppSettings as the arguments
        /// </summary>
        /// <param name="appSettings"></param>
        public MockRequestHelper(AppSettings appSettings)
        {
            _AppSettings = appSettings;

            // set the integration type to the one defined in the config
            if (_AppSettings != null && _AppSettings.IntegrationType != null)
                _IntegrationType = _AppSettings.IntegrationType;

            if (string.Compare(_IntegrationType, "Verification", true) == 0)
                LoadPartnerProducts();
        }

        /// <summary>
        /// This method is a placeholder for the Partner to convert the Elliemae Response to their specific payload
        /// </summary>
        /// <param name="data"></param>
        /// <param name="transactionId"></param>
        /// <param name="integrationType"></param>
        /// <returns></returns>
        public JObject SubmitToPartner(JObject data, string transactionId)
        {
            JObject partnerResponse = new JObject();

            var mockSuccessResponsePath = System.IO.Path.GetFullPath(System.IO.Directory.GetCurrentDirectory() + @"\ResponsePayloads\" + this._IntegrationType + @"\PartnerSuccessResponse.json");
            var successResponse = System.IO.File.ReadAllText(mockSuccessResponsePath);

            partnerResponse = JObject.Parse(successResponse);

            return partnerResponse;
        }

        public void LoadPartnerProducts()
        {
            var mockProductPath = System.IO.Path.GetFullPath(System.IO.Directory.GetCurrentDirectory() + @"\ResponsePayloads\" + this._IntegrationType + @"\PartnerProductMapping.json");
            var sProductResponse = System.IO.File.ReadAllText(mockProductPath);

            _PartnerProduct = JObject.Parse(sProductResponse);
        }

        public string GetProductDescription(string productCode)
        {
            string sProductDescription = string.Empty;

            if (string.IsNullOrEmpty(productCode))
                return sProductDescription;

            try
            {
                if (_PartnerProduct != null)
                {
                    var prod = _PartnerProduct.SelectToken(string.Format(jsonProdPath, productCode));

                    if (prod != null)
                    {
                        foreach (JProperty prop in prod)
                        {
                            if (prop.Name == "name")
                            {
                                sProductDescription = prop.Value.ToString();
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                sProductDescription = string.Empty;
            }

            return sProductDescription;
        }


        public bool IsValidProduct(string productCode)
        {
            bool bResult = false;

            try
            {
                var prod = _PartnerProduct.SelectToken(string.Format(jsonProdPath, productCode));

                if (prod != null)
                    bResult = true;
            }
            catch
            {
                bResult = false;
            }

            return bResult;
        }

    }
}
