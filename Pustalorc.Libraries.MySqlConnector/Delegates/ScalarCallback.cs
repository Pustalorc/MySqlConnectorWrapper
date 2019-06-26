namespace Pustalorc.Libraries.MySqlConnector.Delegates
{
    /// <summary>
    ///     Callback for any scalar queries that finished execution.
    /// </summary>
    /// <param name="query">The query that finished execution.</param>
    /// <param name="result">The result of the query</param>
    public delegate void ScalarCallback(string query, object result);
}