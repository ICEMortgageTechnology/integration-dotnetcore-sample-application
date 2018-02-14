using EPC.ReferenceIntegration.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.DataStore
{
    public sealed class TransactionInformationCache
    {
        private static volatile TransactionInformationCache _Instance;
        private static object syncRoot = new Object();
        private static List<OrderInformation> _Cache = null;
        private static string _TransactionCachePath = Path.GetFullPath(System.IO.Directory.GetCurrentDirectory() + @"\DataStore\TransactionInformationCache.json");

        private TransactionInformationCache() { }

        /// <summary>
        /// Signleton Threadsafe Instance of the Transaction Information Cache
        /// </summary>
        public static TransactionInformationCache Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (syncRoot)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new TransactionInformationCache();                           

                            // read the cache from file. In real world scenario, this may come from the DB
                            var existingCache = GetCacheFromFile();

                            if(existingCache == null)
                                _Cache = new List<OrderInformation>();
                            else
                                _Cache = existingCache;
                        }
                    }
                }

                return _Instance;
            }
        }
        
        /// <summary>
        /// This method will add a Transaction into the Cache object if it does not exist and update the value if it exists
        /// </summary>
        /// <param name="value"></param>
        public void Add(OrderInformation value, bool IsMorethanOneOrderPerTrans = false)
        {
            // if the cache does not contain the transaction then add the transaction in the cache
            if(_Cache != null && value != null)
            {
                var itemIndex = 0;
                var existingOrder = _Cache.FirstOrDefault(x => x.OrderId == value.OrderId);
                itemIndex = _Cache.FindIndex(x => x.OrderId == value.OrderId);

                // if the cache doesn't contain an order with the same order id then search with transaction Id
                if (existingOrder == null && !IsMorethanOneOrderPerTrans)
                {
                    existingOrder = _Cache.FirstOrDefault(x => x.TransactionId == value.TransactionId);
                    itemIndex = _Cache.FindIndex(x => x.TransactionId == value.TransactionId);
                }

                if (existingOrder != null)
                {
                    if(itemIndex != -1)
                        _Cache.RemoveAt(itemIndex);
                }

                _Cache.Add(value);

                UpdateCacheInFile(); // Update cache on file
            }
        }

        /// <summary>
        /// This method will return a value for the Transaction if it exists in the cache
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public OrderInformation GetValue(string orderId)
        {
            OrderInformation value = null;

            // check if the cache is not null and contains the orderId
            if (_Cache != null)
                value = _Cache.FirstOrDefault(x => x.OrderId == orderId);

            // Check if the value is null means it does not match the order with order id, we then try it with TransactionId
            if(value == null)
                value = _Cache.FirstOrDefault(x => x.TransactionId == orderId);

            return value;
        }

        /// <summary>
        /// This method is used to remove the key from the Memory Cache
        /// </summary>
        /// <param name="orderId"></param>
        public void Remove(string orderId)
        {
            if (_Cache != null)
            {
                var itemIndex = _Cache.FindIndex(x => x.OrderId == orderId);
                _Cache.RemoveAt(itemIndex);

                UpdateCacheInFile(); // Update cache on file
            }
        }

        /// <summary>
        /// This method will update the cache file whenever there is an update in the cache inside memory
        /// </summary>
        private static void UpdateCacheInFile()
        {   
            File.WriteAllText(_TransactionCachePath, JsonConvert.SerializeObject(_Cache));
        }

        /// <summary>
        /// This method will read and return the list of Transaction Order Information from the cache file
        /// </summary>
        /// <returns></returns>
        private static List<OrderInformation> GetCacheFromFile()
        {
            List<OrderInformation> orderInfoList = null;

            if (File.Exists(_TransactionCachePath))
            {
                var jsonString = File.ReadAllText(_TransactionCachePath);
                orderInfoList = JsonConvert.DeserializeObject<List<OrderInformation>>(jsonString);
            }

            return orderInfoList;
        }
    }
}
