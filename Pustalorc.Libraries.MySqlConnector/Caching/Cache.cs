using System;

namespace Pustalorc.Libraries.MySqlConnector.Caching
{
    // Basic cache object, to be modified to be compliant with latest changes.
    // Requires self-updating using a Timer object.
    public sealed class Cache
    {
        /// <summary>
        ///     The query that this cache object is dedicated to.
        /// </summary>
        public readonly string Query;

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
        public Cache(string query, object output)
        {
            Query = query;
            _output = output;
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
    }
}