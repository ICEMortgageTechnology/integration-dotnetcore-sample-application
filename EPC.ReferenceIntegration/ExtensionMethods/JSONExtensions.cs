using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    }
}
