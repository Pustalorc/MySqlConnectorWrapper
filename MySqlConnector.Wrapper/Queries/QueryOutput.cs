using JetBrains.Annotations;

namespace Pustalorc.MySqlConnector.Wrapper.Queries;

/// <summary>
/// Stores the result from an executed query.
/// </summary>
[UsedImplicitly]
public class QueryOutput<T> where T : class
{
    /// <summary>
    /// The query that was executed and is related to the output.
    /// </summary>
    public readonly Query<T> Query;

    /// <summary>
    /// The output of the executed query.
    /// </summary>
    [UsedImplicitly] public object? Output;

    /// <summary>
    /// Constructs a new output for the query.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="output"></param>
    public QueryOutput(Query<T> query, object? output)
    {
        Query = query;
        Output = output;
    }
}