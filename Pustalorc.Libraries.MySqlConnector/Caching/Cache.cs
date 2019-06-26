using System;
using System.Timers;
using Pustalorc.Libraries.MySqlConnector.Queries;
using Pustalorc.Libraries.MySqlConnector.Queueing;

namespace Pustalorc.Libraries.MySqlConnector.Caching
{
    /// <summary>
    ///     A cached object that stores the result from executing a query.
    /// </summary>
    public sealed class Cache
    {
        private readonly Timer _selfUpdate;

        /// <summary>
        ///     The query that this cache object is dedicated to.
        /// </summary>
        public readonly Query Query;

        /// <summary>
        ///     The exact time this cache object was created.
        /// </summary>
        public readonly DateTime TimeCreated = DateTime.Now;

        /// <summary>
        ///     The internal value of the output.
        /// </summary>
        private object _output;

        /// <summary>
        ///     A cache for a query.
        /// </summary>
        /// <param name="query">The query to be cached.</param>
        /// <param name="output">The output of the query to be cached.</param>
        public Cache(Query query, object output)
        {
            Query = query;
            _output = output;

            // Figure out how to get the configuration in here to setup how much time the self update should wait.
            // Requires reference to the Connector.
            _selfUpdate = new Timer(10);
            _selfUpdate.Elapsed += UpdateSelf;
            _selfUpdate.Start();
        }

        /// <summary>
        ///     The number of times that this cache object's Output has been accessed.
        /// </summary>
        public ulong AccessCount { get; private set; }

        /// <summary>
        ///     The exact time that this cache object was last synced with the data on the server.
        /// </summary>
        public DateTime LastCacheUpdate { get; } = DateTime.Now;

        /// <summary>
        ///     An incrementing counting property for each time the property is accessed.
        /// </summary>
        public object Output
        {
            get
            {
                AccessCount++;
                return _output;
            }
            set => _output = value;
        }

        private void UpdateSelf(object sender, ElapsedEventArgs e)
        {
            switch (Query.QueryType)
            {
                case EQueryType.Reader:
                    // exec reader
                    break;
                case EQueryType.Scalar:
                    // exec scalar
                    break;
            }
        }
    }
}