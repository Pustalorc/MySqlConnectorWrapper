using System;
using System.Collections.Generic;
using System.Data.Common;
using JetBrains.Annotations;

namespace Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.Execution;

[UsedImplicitly]
public class Query
{
    public string QueryString { get; }
    public EQueryType Type { get; }
    public IEnumerable<DbParameter> Parameters { get; }
    public Action<QueryOutput, DbConnection, DbTransaction?>? Callback { get; }

    public Query(string queryString, EQueryType type = EQueryType.NonQuery,
        Action<QueryOutput, DbConnection, DbTransaction?>? callback = null, params DbParameter[] parameters)
    {
        QueryString = queryString;
        Type = type;
        Parameters = parameters;
        Callback = callback;
    }
}