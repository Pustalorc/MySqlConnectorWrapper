using System.Collections.Generic;
using JetBrains.Annotations;
using MySqlConnector;

namespace Pustalorc.MySqlConnector.Wrapper.Queries
{
    /// <summary>
    /// Base class for a query.
    /// </summary>
    [UsedImplicitly]
    public class Query<T> where T : class
    {
        /// <summary>
        /// The key to identify this query.
        /// </summary>
        public T Key { get; }

        /// <summary>
        /// The string of the query that gets executed in MySql.
        /// </summary>
        public string QueryString { get; }

        /// <summary>
        /// Callback for any queries that finished execution.
        /// </summary>
        /// <param name="queryOutput">The output of the executed query, including the query object.</param>
        public delegate void QueryCallback(QueryOutput<T> queryOutput);

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
        /// Determines whether the query should be cached or not. Does not cache if caching is disabled in configuration.
        /// </summary>
        public bool ShouldCache { get; }

        /// <summary>
        /// Constructor for the query.
        /// </summary>
        /// <param name="key">The key for this query.</param>
        /// <param name="query">The string of the query to execute.</param>
        /// <param name="type">The type of the query.</param>
        /// <param name="callbacks">The callback for when execution of the query is finished.</param>
        /// <param name="shouldCache">If the query should be cached or not after executing.</param>
        /// <param name="parameters">The parameters for the query.</param>
        public Query(T key, string query, EQueryType type, bool shouldCache = false,
            IEnumerable<MySqlParameter>? parameters = null, IEnumerable<QueryCallback>? callbacks = null)
        {
            Key = key;
            QueryString = query;
            Type = type;
            ShouldCache = shouldCache;
            Parameters = parameters ?? new List<MySqlParameter>();
            Callbacks = callbacks ?? new List<QueryCallback>();
        }
    }
}