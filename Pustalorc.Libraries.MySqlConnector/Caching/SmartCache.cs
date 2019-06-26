using System;
using System.Collections.Generic;
using Pustalorc.Libraries.MySqlConnector.Configuration;

namespace Pustalorc.Libraries.MySqlConnector.Caching
{
    public sealed class SmartCache<T> where T : IConnectorConfiguration
    {
        private readonly List<Cache> _cache = new List<Cache>();
        private readonly Connector<T> _connector;
        private readonly int _maxCacheSize;

        public SmartCache(Connector<T> connector)
        {
            _connector = connector;
            _maxCacheSize = connector.Configuration.MaxCacheSize;
        }

        public Cache GetItemInCache(string query)
        {
            return _cache.Find(k =>
                !string.IsNullOrEmpty(k?.Query) &&
                string.Equals(k.Query, query, StringComparison.InvariantCultureIgnoreCase));
        }

        private int GetUselessIndex()
        {
            return 1;
        }

        public void UpdateStoreItemInCache(string query, object output)
        {
            var cache = GetItemInCache(query);
            if (cache == null)
            {
                var item = new Cache(query, output);

                if (_cache.Count == _maxCacheSize)
                {
                    var index = GetUselessIndex();
                    _cache[index] = item;
                }

                _cache.Add(item);
            }

            _cache[_cache.IndexOf(cache)].Output = output;
        }
    }
}