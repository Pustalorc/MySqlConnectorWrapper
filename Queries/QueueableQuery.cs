using Pustalorc.Libraries.MySqlConnector.Delegates;

namespace Pustalorc.Libraries.MySqlConnector.Queries
{
    /// <summary>
    ///     Base class for a query that can be queued.
    /// </summary>
    public sealed class QueueableQuery
    {
        /// <summary>
        ///     The query object being queued.
        /// </summary>
        public readonly Query Query;

        /// <summary>
        ///     The type of query (and execution) that this object defines.
        /// </summary>
        public readonly QueryCallback QueryCallback;

        /// <summary>
        ///     Constructor for the base class of a query that can be queued.
        /// </summary>
        /// <param name="query">The query object being queued.</param>
        /// <param name="callback">The method to be executed upon query execution completion.</param>
        public QueueableQuery(Query query, QueryCallback callback)
        {
            Query = query;
            QueryCallback = callback;
        }
    }
}