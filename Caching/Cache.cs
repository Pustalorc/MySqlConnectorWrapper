using Pustalorc.Libraries.MySqlConnectorWrapper.Queries;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Caching
{
    /// <summary>
    ///     A cached object that stores the result from executing a query.
    /// </summary>
    public sealed class Cache
    {
        /// <summary>
        ///     The query that this cache object is dedicated to.
        /// </summary>
        public readonly Query Query;

        /// <summary>
        ///     The value of the output.
        /// </summary>
        public object Output;

        /// <summary>
        ///     A cache for a query.
        /// </summary>
        /// <param name="query">The query to be cached.</param>
        /// <param name="output">The output of the query to be cached.</param>
        public Cache(Query query, object output)
        {
            Query = query;
            Output = output;
        }
    }
}