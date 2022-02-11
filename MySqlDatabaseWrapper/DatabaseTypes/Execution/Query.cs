using System;
using System.Collections.Generic;
using System.Data.Common;
using JetBrains.Annotations;
using MySql.Data.MySqlClient;

namespace Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.Execution;

/// <summary>
/// Base class to wrap the basic requirements of a query.
/// </summary>
/// <remarks>
/// A callback is included in order to do further synchronous query calls right after the current query has run.
/// </remarks>
[UsedImplicitly]
public class Query
{
    /// <summary>
    /// The string of the query that gets executed in MySql.
    /// </summary>
    /// <remarks>
    /// An example of what makes a query string:
    /// SELECT * FROM `SampleTable` WHERE `Id`=@id;
    /// </remarks>
    public string QueryString { get; }

    /// <summary>
    /// The type of the query that will be executed.
    /// </summary>
    /// <remarks>
    /// For more information, see <see cref="EQueryType"/>
    /// </remarks>
    public EQueryType Type { get; }

    /// <summary>
    /// The parameters to be used on the underlying DbCommand to bind into the query string.
    /// </summary>
    /// <remarks>
    /// This field will require a different type to be used. For example, if the wrapped connector uses MySql.Data, then <see cref="MySqlParameter"/> should be used.
    /// </remarks>
    public IEnumerable<DbParameter> Parameters { get; }

    /// <summary>
    /// The callback to run after the query has executed.
    /// </summary>
    /// <remarks>
    /// This callback passes DbConnection and DbTransaction to allow for further synchronous calls after the main query runs.
    /// For example: you run a query to check if a table exists, and subsequent queries, ran in the callback create the table if it doesnt and fill it.
    /// 
    /// Note that throwing an exception during a callback when the callback was from a ExecuteTransaction, will result in the transaction rolling back and not commit any of the data on the transaction.
    /// This does not occur when using ExecuteQuery, as no transaction is started by default there. This behaviour can be overriden.
    /// </remarks>
    public Action<QueryOutput, DbConnection, DbTransaction?>? Callback { get; }

    /// <summary>
    /// Construct a new query with the provided information.
    /// </summary>
    /// <param name="queryString">The string of the query that gets executed in MySql.</param>
    /// <param name="type">The type of the query that will be executed.</param>
    /// <param name="callback">The callback to run after the query has executed.</param>
    /// <param name="parameters">The parameters to be used on the underlying DbCommand to bind into the query string.</param>
    /// <remarks>
    /// Due to most queries not having a result or not needing one other than records affected, <see cref="EQueryType.NonQuery"/> is the default type for all queries.
    /// </remarks>
    public Query(string queryString, EQueryType type = EQueryType.NonQuery,
        Action<QueryOutput, DbConnection, DbTransaction?>? callback = null, params DbParameter[] parameters)
    {
        QueryString = queryString;
        Type = type;
        Parameters = parameters;
        Callback = callback;
    }
}