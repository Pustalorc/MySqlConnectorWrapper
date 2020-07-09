using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Pustalorc.Libraries.FrequencyCache.Interfaces;

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
        /// <param name="callbacks">The callback for when execution of the query is finished.</param>
        /// <param name="shouldCache">If the query should be cached or not after executing.</param>
        /// <param name="parameters">The parameters for the query.</param>
        public Query(object uniqueIdentifier, string query, EQueryType type, bool shouldCache = false,
            IEnumerable<MySqlParameter> parameters = null, IEnumerable<QueryCallback> callbacks = null)
        {
            UniqueIdentifier = uniqueIdentifier?.ToString() ?? query;
            QueryString = query;
            Type = type;
            ShouldCache = shouldCache;
            Parameters = parameters ?? new List<MySqlParameter>();
            Callbacks = callbacks ?? new List<QueryCallback>();
        }

        #region Overloads for string + type

        public Query(string query, EQueryType type) : this(uniqueIdentifier: null, query, type)
        {
        }

        public Query(string query, EQueryType type, params MySqlParameter[] parameters) : this(uniqueIdentifier: null,
            query, type, false, parameters)
        {
        }

        public Query(string query, EQueryType type, params QueryCallback[] callbacks) : this(uniqueIdentifier: null,
            query, type, false, null, callbacks)
        {
        }

        #endregion

        #region Overloads for string + type + IEnumerable<T>

        public Query(string query, EQueryType type, IEnumerable<MySqlParameter> parameters, params QueryCallback[] callbacks) : this(uniqueIdentifier: null, query, type, false, parameters, callbacks)
        {
        }

        public Query(string query, EQueryType type, IEnumerable<QueryCallback> callbacks, params MySqlParameter[] parameters) : this(uniqueIdentifier: null, query, type, false, parameters, callbacks)
        {
        }

        #endregion

        #region Overloads for string + type + bool

        public Query(string query, EQueryType type, bool shouldCache) : this(uniqueIdentifier: null, query, type,
            shouldCache)
        {
        }

        public Query(string query, EQueryType type, bool shouldCache, params MySqlParameter[] parameters) : this(
            uniqueIdentifier: null, query, type, shouldCache, parameters)
        {
        }

        public Query(string query, EQueryType type, bool shouldCache, params QueryCallback[] callbacks) : this(
            uniqueIdentifier: null, query, type, shouldCache, null, callbacks)
        {
        }

        #endregion

        #region Overloads for string + type + bool + IEnumerable<T>

        public Query(string query, EQueryType type, bool shouldCache, IEnumerable<MySqlParameter> parameters,
            params QueryCallback[] callbacks) : this(uniqueIdentifier: null, query, type, shouldCache, parameters,
            callbacks)
        {
        }

        public Query(string query, EQueryType type, bool shouldCache, IEnumerable<QueryCallback> callbacks,
            params MySqlParameter[] parameters) : this(null, query, type, shouldCache, parameters, callbacks)
        {
        }

        #endregion

        #region Overloads for object + string + type

        public Query(object identifier, string query, EQueryType type) : this(uniqueIdentifier: identifier, query, type)
        {
        }

        public Query(object identifier, string query, EQueryType type, params MySqlParameter[] parameters) : this(
            uniqueIdentifier: identifier, query, type, false, parameters)
        {
        }

        public Query(object identifier, string query, EQueryType type, params QueryCallback[] callbacks) : this(
            uniqueIdentifier: identifier, query, type, false, null, callbacks)
        {
        }

        #endregion

        #region Overloads for object + string + type + IEnumerable<T>

        public Query(object identifier, string query, EQueryType type, IEnumerable<QueryCallback> callbacks, params MySqlParameter[] parameters) : this(uniqueIdentifier: identifier, query, type, false, parameters, callbacks)
        {
        }

        public Query(object identifier, string query, EQueryType type,IEnumerable<MySqlParameter> parameters, params QueryCallback[] callbacks) : this(uniqueIdentifier: identifier, query, type, false, parameters, callbacks)
        {
        }

        #endregion

        #region Overloads for object + string + type + bool

        public Query(object identifier, string query, EQueryType type, bool shouldCache) : this(identifier, query, type,
            shouldCache, new List<MySqlParameter>())
        {
        }

        public Query(object identifier, string query, EQueryType type, bool shouldCache,
            params MySqlParameter[] parameters) : this(uniqueIdentifier: identifier, query, type, shouldCache,
            parameters)
        {
        }

        public Query(object identifier, string query, EQueryType type, bool shouldCache,
            params QueryCallback[] callbacks) : this(uniqueIdentifier: identifier, query, type, shouldCache, null,
            callbacks)
        {
        }

        #endregion

        #region Overloads for object + string + type + bool + IEnumerable<T>

        public Query(object identifier, string query, EQueryType type, bool shouldCache,
            IEnumerable<QueryCallback> callbacks, params MySqlParameter[] parameters) : this(identifier, query, type,
            shouldCache, parameters, callbacks)
        {
        }

        public Query(object identifier, string query, EQueryType type, bool shouldCache,
            IEnumerable<MySqlParameter> parameters, params QueryCallback[] callbacks) : this(
            uniqueIdentifier: identifier, query, type, shouldCache, parameters, callbacks)
        {
        }

        #endregion
    }
}