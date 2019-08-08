using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Pustalorc.Libraries.MySqlConnectorWrapper.Configuration;
using Pustalorc.Libraries.MySqlConnectorWrapper.Queries;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Caching
{
    public class CacheManager<T> where T : IConnectorConfiguration
    {
        /// <summary>
        ///     Timer to request the database for updates on the cached items.
        /// </summary>
        private readonly Timer _selfUpdate;

        /// <summary>
        ///     The list of cached queries.
        /// </summary>
        private readonly List<Cache> _cache = new List<Cache>();

        /// <summary>
        ///     The instance of the connector.
        /// </summary>
        private readonly ConnectorWrapper<T> _connector;

        /// <summary>
        ///     Instantiates the cache manager. Requires the instance of the connector.
        /// </summary>
        /// <param name="connector">The instance of the connector being used.</param>
        internal CacheManager(ConnectorWrapper<T> connector)
        {
            _connector = connector;

            _selfUpdate = new Timer(connector.Configuration.CacheRefreshIntervalMilliseconds);
            _selfUpdate.Elapsed += UpdateCacheItems;
            _selfUpdate.Start();
        }

        /// <summary>
        ///     Removes a specific item from the cache, based on the query input.
        /// </summary>
        /// <param name="query">The query related to the item in cache to be removed.</param>
        /// <returns>If it successfully removed the item from the cache.</returns>
        public bool RemoveItemFromCache(Query query)
        {
            return RemoveItemFromCache(GetItemInCache(query));
        }

        /// <summary>
        ///     Removes the specified item from cache.
        /// </summary>
        /// <param name="cache">The item to remove from cache.</param>
        /// <returns>If it successfully removed the item from the cache.</returns>
        public bool RemoveItemFromCache(Cache cache)
        {
            return _cache.Remove(cache);
        }

        /// <summary>
        ///     Gets the item from the cache based on the input query.
        /// </summary>
        /// <param name="query">The query related to the item in cache to be retrieved.</param>
        /// <returns>The cache item if it is found or null otherwise.</returns>
        public Cache GetItemInCache(Query query)
        {
            return _cache.FirstOrDefault(k => k.Query == query);
        }

        /// <summary>
        ///     Stores a new item in cache with the input query and output.
        /// </summary>
        /// <param name="query">The query that was executed.</param>
        /// <param name="output">The output of said query.</param>
        public void StoreItemInCache(Query query, object output)
        {
            var cache = GetItemInCache(query);
            if (cache != null) return;

            var item = new Cache(query, output);
            _cache.Add(item);
        }

        private void UpdateCacheItems(object sender, ElapsedEventArgs e)
        {
            foreach (var c in _cache) c.Output = _connector.ExecuteQuery(c.Query);
        }
    }
}