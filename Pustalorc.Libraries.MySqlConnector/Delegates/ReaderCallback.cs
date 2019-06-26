using System.Collections.Generic;
using Pustalorc.Libraries.MySqlConnector.TableStructure;

namespace Pustalorc.Libraries.MySqlConnector.Delegates
{
    /// <summary>
    ///     Callback for any reader queries that finished execution.
    /// </summary>
    /// <param name="query">The query that finished execution.</param>
    /// <param name="result">The result of the query</param>
    public delegate void ReaderCallback(string query, IEnumerable<Row> result);
}