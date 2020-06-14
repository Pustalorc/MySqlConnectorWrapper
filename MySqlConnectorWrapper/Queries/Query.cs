using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Queries
{
    /// <summary>
    ///     Base class for a query.
    /// </summary>
    public sealed class Query
    {
        /// <summary>
        ///     The unique identifier for this query. If equal to another, it will be used to replace its retrieved data. Use with care.
        /// </summary>
        public readonly object Identifier;

        /// <summary>
        ///     The callback for when execution of this query is finished.
        /// </summary>
        public readonly QueryCallback QueryCallback;

        /// <summary>
        ///     The parameters to be added to the command prior to execution.
        /// </summary>
        public readonly IEnumerable<MySqlParameter> QueryParameters;

        /// <summary>
        ///     The string of the query that gets executed in MySql.
        /// </summary>
        public readonly string QueryString;

        /// <summary>
        ///     The type of the query that will be executed.
        /// </summary>
        public readonly EQueryType QueryType;

        /// <summary>
        ///     Determines whether the query should be cached or not (only affected if caching is enabled).
        /// </summary>
        public readonly bool ShouldCache;

        /// <summary>
        ///     Constructor for the query.
        /// </summary>
        /// <param name="identifier">The unique identifier for this query. If equal to another queries', it will be used to replace its retrieved data.</param>
        /// <param name="query">The string of the query to execute.</param>
        /// <param name="type">The type of the query.</param>
        /// <param name="callback">The callback for when execution of the query is finished.</param>
        /// <param name="shouldCache">If the query should be cached or not after executing.</param>
        /// <param name="queryParameters">The parameters for the query.</param>
        public Query(object identifier, string query, EQueryType type, QueryCallback callback = null,
            bool shouldCache = false,
            params MySqlParameter[] queryParameters)
        {
            Identifier = identifier;
            QueryString = query;
            QueryType = type;
            QueryCallback = callback;
            ShouldCache = shouldCache;
            QueryParameters = queryParameters;
        }
    }
}