using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.DataStore
{
    public sealed class TransactionStatusCache
    {
        private static volatile TransactionStatusCache _Instance;
        private static object syncRoot = new Object();
        private static Dictionary<string, object> _Cache = null;

        private TransactionStatusCache() { }

        /// <summary>
        /// Signleton Threadsafe Instance of the Transaction Cache
        /// </summary>
        public static TransactionStatusCache Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (syncRoot)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new TransactionStatusCache();
                            _Cache = new Dictionary<string, object>();
                        }
                    }
                }

                return _Instance;
            }
        }

        /// <summary>
        /// This method will add a Transaction into the Cache object if it does not exist and update the value if it exists
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, object value)
        {
            // if the cache does not contain the transaction then add the transaction in the cache
            if (_Cache != null && !_Cache.ContainsKey(key))
                _Cache.Add(key, value);
            else if (_Cache != null && _Cache.ContainsKey(key)) // if the cache contains a given transaction then update the transaction value in the cache
                _Cache[key] = value;
        }

        /// <summary>
        /// This method will return a value for the Transaction if it exists in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetValue(string key)
        {
            object value = null;

            // check if the cache is not null and contains the key
            if (_Cache != null && _Cache.ContainsKey(key))
                value = _Cache[key];

            return value;
        }

        /// <summary>
        /// This method is used to remove the key from the Memory Cache
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            if (_Cache != null && _Cache.ContainsKey(key))
                _Cache.Remove(key);
        }
    }
}
