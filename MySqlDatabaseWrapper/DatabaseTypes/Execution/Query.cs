using System;
using System.Collections.Generic;
using System.Data.Common;
using JetBrains.Annotations;
using MySql.Data.MySqlClient;

namespace Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.Execution;

[UsedImplicitly]
public class Query
{
    public string QueryString { get; }
    public EQueryType Type { get; }
    public bool ShouldCache { get; }
    public IEnumerable<MySqlParameter> Parameters { get; }
    public Action<QueryOutput, DbConnection>? Callback { get; }

    public Query(string queryString, EQueryType type = EQueryType.NonQuery, bool shouldCache = false,
        Action<QueryOutput, DbConnection>? callback = null, params MySqlParameter[] parameters)
    {
        QueryString = queryString;
        Type = type;
        ShouldCache = shouldCache;
        Parameters = parameters;
        Callback = callback;
    }
}