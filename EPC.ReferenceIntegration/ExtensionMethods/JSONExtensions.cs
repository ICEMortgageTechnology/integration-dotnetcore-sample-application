using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

namespace EPC.ReferenceIntegration.ExtensionMethods
{
    public static class JSONExtensions
    {
        /// <summary>
        /// Gets value by key and converts it in to a Generic Type Object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jToken"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetValue<T>(this JToken jToken, string key, T defaultValue = default(T))
        {
            dynamic ret = jToken[key];
            if (ret == null) return defaultValue;
            if (ret is JObject) return JsonConvert.DeserializeObject<T>(ret.ToString());
            return (T)ret;
        }

        /// <summary>
        /// This methid will check if the JSON is valid (it will check if it has any script tags)
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static bool IsValidJSON(this JObject payload)
        {
            if(payload != null)
            {
                var inputParameter = payload.ToString();

                if (string.IsNullOrEmpty(inputParameter))
                    return true;

                var pattern = new StringBuilder();

                //Checks any js events i.e. onKeyUp(), onBlur(), alerts and custom js functions etc.             
                pattern.Append(@"((alert|on\w+|function\s+\w+)\s*\(\s*(['+\d\w](,?\s*['+\d\w]*)*)*\s*\))");

                //Checks any html tags i.e. <script, <embed, <object etc.
                pattern.Append(@"|(<(script|iframe|embed|frame|frameset|object|img|applet|body|html|style|layer|link|ilayer|meta|bgsound))");

                return !Regex.IsMatch(WebUtility.UrlDecode(inputParameter), pattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            return false;
        }
    }
}
