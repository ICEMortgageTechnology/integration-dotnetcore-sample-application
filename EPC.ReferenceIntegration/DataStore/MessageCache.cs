using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.DataStore
{
    /// <summary>
    /// This class is to maintain an in-memory repository for messages.
    /// It is basically a Dictionary which holds the message ID + message. In real world scenarios this would come from a Database or some data store at the partner's end.
    /// </summary>
    public sealed class MessageCache
    {
        private static volatile MessageCache _Instance;
        private static object syncRoot = new Object();
        private static Dictionary<string, string> _Cache = null;

        private MessageCache() { }

        /// <summary>
        /// Signleton Threadsafe Instance of the Message Cache
        /// </summary>
        public static MessageCache Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (syncRoot)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new MessageCache();
                            _Cache = new Dictionary<string, string>();
                        }
                    }
                }

                return _Instance;
            }
        }

        /// <summary>
        /// This method will add a Message into the Cache object if it does not exist and update the value if it exists
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, string value)
        {
            // if the cache does not contain the transaction then add the transaction in the cache
            if (_Cache != null && !_Cache.ContainsKey(key))
                _Cache.Add(key, value);
            else if (_Cache != null && _Cache.ContainsKey(key)) // if the cache contains a given transaction then update the transaction value in the cache
                _Cache[key] = value;
        }

        /// <summary>
        /// This method will return a value for the Message if it exists in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetValue(string key)
        {
            string value = null;

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
