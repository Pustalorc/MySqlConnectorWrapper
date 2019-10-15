namespace Pustalorc.Libraries.MySqlConnectorWrapper.Queries
{
    /// <summary>
    ///     Stores the result from an executed query.
    /// </summary>
    public sealed class QueryOutput
    {
        /// <summary>
        ///     The query that was executed and is related to the output.
        /// </summary>
        public readonly Query Query;

        /// <summary>
        ///     The output of the executed query.
        /// </summary>
        public object Output;

        public QueryOutput(Query query, object output)
        {
            Query = query;
            Output = output;
        }
    }
}