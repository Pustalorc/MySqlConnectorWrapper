using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using Pustalorc.Libraries.FrequencyCache;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Queries
{
    /// <summary>
    /// Base class for a query.
    /// </summary>
    public sealed class Query : IIdentifiable
    {
        /// <summary>
        /// The unique identifier of this query. Used for comparisons in the cache.
        /// </summary>
        public string UniqueIdentifier { get; }

        /// <summary>
        /// The string of the query that gets executed in MySql.
        /// </summary>
        public string QueryString { get; }

        /// <summary>
        /// The callback for when execution of this query is finished.
        /// </summary>
        public IEnumerable<QueryCallback> Callbacks { get; }

        /// <summary>
        /// The parameters to be swapped in the QueryString prior to execution.
        /// </summary>
        public IEnumerable<MySqlParameter> Parameters { get; }

        /// <summary>
        /// The type of the query that will be executed.
        /// </summary>
        public EQueryType Type { get; }

        /// <summary>
        /// Determines whether the query should be cached or not (only affected if caching is enabled).
        /// </summary>
        public bool ShouldCache { get; }

        /// <summary>
        /// Constructor for the query.
        /// </summary>
        /// <param name="uniqueIdentifier">The unique identifier for this query.</param>
        /// <param name="query">The string of the query to execute.</param>
        /// <param name="type">The type of the query.</param>
        /// <param name="callback">The callback for when execution of the query is finished.</param>
        /// <param name="shouldCache">If the query should be cached or not after executing.</param>
        /// <param name="queryParameters">The parameters for the query.</param>
        public Query(object uniqueIdentifier, string query, EQueryType type, bool shouldCache = false,
            IEnumerable<MySqlParameter> queryParameters = null, params QueryCallback[] callback)
        {
            UniqueIdentifier = uniqueIdentifier?.ToString() ?? query;
            QueryString = query;
            Type = type;
            ShouldCache = shouldCache;
            Parameters = queryParameters ?? new List<MySqlParameter>();
            Callbacks = callback;
        }
    }
}