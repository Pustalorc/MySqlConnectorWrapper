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
        public string QueryString;

        /// <summary>
        ///     The type of the query that will be executed.
        /// </summary>
        public EQueryType QueryType;

        /// <summary>
        ///     The parameters to be added to the command prior to execution.
        /// </summary>
        public List<QueryParameter> QueryParameters;

        /// <summary>
        ///     Constructor for the query.
        /// </summary>
        /// <param name="query">The string of the query to execute.</param>
        /// <param name="type">The type of the query.</param>
        /// <param name="queryParameters">The parameters for the query.</param>
        public Query(string query, EQueryType type, params QueryParameter[] queryParameters)
        {
            QueryString = query;
            QueryType = type;
            QueryParameters = queryParameters.ToList();
        }
    }
}