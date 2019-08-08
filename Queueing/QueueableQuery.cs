using Pustalorc.Libraries.MySqlConnectorWrapper.Delegates;
using Pustalorc.Libraries.MySqlConnectorWrapper.Queries;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Queueing
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
        ///     The callback to be called when execution of the query completes.
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