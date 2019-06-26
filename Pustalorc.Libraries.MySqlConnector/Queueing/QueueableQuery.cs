namespace Pustalorc.Libraries.MySqlConnector.Queueing
{
    /// <summary>
    ///     Base class for a query that can be queued.
    /// </summary>
    public sealed class QueueableQuery
    {
        /// <summary>
        ///     The query in question being queued.
        /// </summary>
        public readonly string Query;

        /// <summary>
        ///     The type of query (and execution) that this object defines.
        /// </summary>
        public readonly EQueueableQueryType QueryQueryType;

        /// <summary>
        ///     Constructor for the base class of a query that can be queued.
        /// </summary>
        /// <param name="query">The query in question being queued.</param>
        /// <param name="queryType">The type of query (and execution) that this object defines.</param>
        public QueueableQuery(string query, EQueueableQueryType queryType)
        {
            Query = query;
            QueryQueryType = queryType;
        }
    }
}