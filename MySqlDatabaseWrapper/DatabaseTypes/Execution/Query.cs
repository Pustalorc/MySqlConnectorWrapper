using System;
using System.Collections.Generic;
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
    public Action<QueryOutput, MySqlConnection>? MySqlDataCallback { get; }
    public Action<QueryOutput, MySqlConnector.MySqlConnection>? MySqlConnectorCallback { get; }

    public Query(string queryString, EQueryType type = EQueryType.NonQuery, bool shouldCache = false,
        Action<QueryOutput, MySqlConnection>? mySqlDataCallback = null,
        Action<QueryOutput, MySqlConnector.MySqlConnection>? mySqlConnectorCallback = null,
        params MySqlParameter[] parameters)
    {
        QueryString = queryString;
        Type = type;
        ShouldCache = shouldCache;
        Parameters = parameters;
        MySqlDataCallback = mySqlDataCallback;
        MySqlConnectorCallback = mySqlConnectorCallback;
    }
}