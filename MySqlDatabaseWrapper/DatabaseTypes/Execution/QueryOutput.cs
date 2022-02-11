using System.Collections.Generic;
using System.Data.Common;
using JetBrains.Annotations;
using Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.ResultTable;

namespace Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.Execution;

/// <summary>
/// Base class to wrap the results from queries, with methods to help retrieving the information back.
/// </summary>
[UsedImplicitly]
public class QueryOutput
{
    /// <summary>
    /// The query that was executed, with its details.
    /// </summary>
    [UsedImplicitly]
    public Query Query { get; }

    /// <summary>
    /// The output from <see cref="DbCommand"/> after executing the query.
    /// </summary>
    /// <remarks>
    /// This is type object? because that is what DbCommand.ExecuteScalar() returns.
    /// Null is a possibility.
    /// </remarks>
    [UsedImplicitly]
    public object? Result { get; set; }

    /// <summary>
    /// Construct a new output for a query.
    /// </summary>
    /// <param name="query">The query that was executed, with its details.</param>
    /// <param name="result">The output from <see cref="DbCommand"/> after executing the query.</param>
    public QueryOutput(Query query, object? result)
    {
        Query = query;
        Result = result;
    }

    /// <summary>
    /// Attempts to get a value of type T from the result in this class.
    /// </summary>
    /// <param name="defaultIfNot">A default value in the scenario where result is not of type T.</param>
    /// <typeparam name="T">The type to get or check from the result.</typeparam>
    /// <returns>The instance of type T from the result, or the value from defaultIfNot if the result is not of type T.</returns>
    public T? GetTFromResult<T>(T? defaultIfNot = default)
    {
        return Result is T t ? t : defaultIfNot;
    }

    /// <summary>
    /// Gets the reader result from the query, as a <see cref="List{Row}"/>
    /// </summary>
    /// <returns>
    /// An empty <see cref="List{Row}"/>, or a <see cref="List{Row}"/> with all the result rows from the reader query.
    /// </returns>
    [UsedImplicitly]
    public List<Row> GetReaderResult()
    {
        return GetTFromResult(new List<Row>())!;
    }

    /// <summary>
    /// Gets the non query result from the query, which is the number of rows affected, as an integer, by default of int type.
    /// </summary>
    /// <returns>
    /// An integer of the number of affected rows.
    /// In the case where the result is not an integer, for example when the query wasn't a non-query type, int.MaxValue is returned.
    /// </returns>
    [UsedImplicitly]
    public int GetNonQueryResult()
    {
        return GetTFromResult(int.MaxValue);
    }

    /// <summary>
    /// Checks if the value in result is null.
    /// </summary>
    /// <returns>
    /// True if the value is null, false otherwise.
    /// </returns>
    [UsedImplicitly]
    public bool IsResultNull()
    {
        return Result is null;
    }
}