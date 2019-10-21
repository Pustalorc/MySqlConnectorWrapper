using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Pustalorc.Libraries.MySqlConnectorWrapper.Configuration;
using Pustalorc.Libraries.MySqlConnectorWrapper.Queries;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Caching
{
    public sealed class CacheManager<T> : IDisposable where T : IConnectorConfiguration
    {
        /// <summary>
        ///     The list of cached queries.
        /// </summary>
        private readonly List<QueryOutput> _cache = new List<QueryOutput>();

        /// <summary>
        ///     The instance of the connector.
        /// </summary>
        private readonly ConnectorWrapper<T> _connector;

        /// <summary>
        ///     Timer to request the database for updates on the cached items.
        /// </summary>
        private readonly Timer _selfUpdate;

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

        public void Dispose()
        {
            _selfUpdate?.Dispose();
            _cache.Clear();
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
        /// <param name="queryOutput">The item to remove from cache.</param>
        /// <returns>If it successfully removed the item from the cache.</returns>
        private bool RemoveItemFromCache(QueryOutput queryOutput)
        {
            return _cache.Remove(queryOutput);
        }

        /// <summary>
        ///     Gets the item from the cache based on the input query.
        /// </summary>
        /// <param name="query">The query related to the item in cache to be retrieved.</param>
        /// <returns>The cache item if it is found or null otherwise.</returns>
        public QueryOutput GetItemInCache(Query query)
        {
            return _cache.FirstOrDefault(k =>
                k.Query.QueryString.Equals(query.QueryString, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        ///     Stores a new item in cache with the input query and output.
        /// </summary>
        /// <param name="queryOutput">The output of said query.</param>
        public void StoreUpdateItemInCache(QueryOutput queryOutput)
        {
            var cache = GetItemInCache(queryOutput.Query);
            if (cache != null) cache.Output = queryOutput.Output;
            else _cache.Add(queryOutput);
        }

        private void UpdateCacheItems(object sender, ElapsedEventArgs e)
        {
            _connector.ExecuteTransaction(_cache.Select(k => k.Query).ToArray());
        }

        /// <summary>
        ///     Updates the cache's timer with a new interval.
        /// </summary>
        /// <param name="time">The new interval (in ms).</param>
        public void UpdateCacheRefreshTime(double time)
        {
            _selfUpdate.Stop();
            _selfUpdate.Interval = time;
            _selfUpdate.Start();
        }
    }
}