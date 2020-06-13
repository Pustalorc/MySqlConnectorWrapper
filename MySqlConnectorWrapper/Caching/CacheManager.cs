using System;
using System.Linq;
using System.Timers;
using Pustalorc.Libraries.MySqlConnectorWrapper.Configuration;
using Pustalorc.Libraries.MySqlConnectorWrapper.Queries;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Caching
{
    public class CachedQuery : QueryOutput
    {
        public int AccessCount;

        public CachedQuery(QueryOutput query) : base(query.Query, query.Output)
        {
        }
    }

    public sealed class CacheManager<T> : IDisposable where T : IConnectorConfiguration
    {
        /// <summary>
        ///     The array of cached queries.
        /// </summary>
        private readonly CachedQuery[] _cache;

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

            _cache = new CachedQuery[connector.Configuration.CacheSize];

            _selfUpdate = new Timer(connector.Configuration.CacheRefreshIntervalMilliseconds);
            _selfUpdate.Elapsed += UpdateCacheItems;
            _selfUpdate.Start();
        }

        public void Dispose()
        {
            _selfUpdate?.Dispose();
            Array.Clear(_cache, 0, _cache.Length);
        }

        /// <summary>
        ///     Gets the item from the cache based on the input query.
        /// </summary>
        /// <param name="query">The query related to the item in cache to be retrieved.</param>
        /// <returns>The cache item if it is found or null otherwise.</returns>
        public QueryOutput GetItemInCache(Query query)
        {
            return _cache.FirstOrDefault(k => k != null && k.Query.QueryString.Equals(query.QueryString, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Stores a new item in cache with the input query and output.
        /// </summary>
        /// <param name="queryOutput">The output of said query.</param>
        public void StoreUpdateItemInCache(QueryOutput queryOutput)
        {
            var cache = GetItemInCache(queryOutput.Query);
            if (cache != null) cache.Output = queryOutput.Output;
            else _cache[GetBestCacheIndex()] = new CachedQuery(queryOutput);
        }

        private int GetBestCacheIndex()
        {
            var index = _cache.FindFirstIndexNull();
            if (index > -1)
                return index;

            index = _cache.IndexOfLeastUse();

            if (index > -1)
                return index;

            throw new Exception("No valid index was found, cannot use -1");
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