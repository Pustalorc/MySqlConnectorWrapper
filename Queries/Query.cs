using System.Collections.Generic;
using System.Linq;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Queries
{
    /// <summary>
    ///     Base class for a query.
    /// </summary>
    public sealed class Query
    {
        /// <summary>
        ///     The string of the query that gets executed in MySql.
        /// </summary>
        public readonly string QueryString;

        /// <summary>
        ///     The type of the query that will be executed.
        /// </summary>
        public readonly EQueryType QueryType;

        /// <summary>
        ///     The parameters to be added to the command prior to execution.
        /// </summary>
        public readonly List<QueryParameter> QueryParameters;

        /// <summary>
        ///     Determines whether the query should be cached or not (only affected if caching is enabled).
        /// </summary>
        public readonly bool ShouldCache;

        /// <summary>
        ///     Constructor for the query.
        /// </summary>
        /// <param name="query">The string of the query to execute.</param>
        /// <param name="type">The type of the query.</param>
        /// <param name="shouldCache">If the query should be cached or not after executing.</param>
        /// <param name="queryParameters">The parameters for the query.</param>
        public Query(string query, EQueryType type, bool shouldCache = false, params QueryParameter[] queryParameters)
        {
            QueryString = query;
            QueryType = type;
            ShouldCache = shouldCache;
            QueryParameters = queryParameters.ToList();
        }
    }
}