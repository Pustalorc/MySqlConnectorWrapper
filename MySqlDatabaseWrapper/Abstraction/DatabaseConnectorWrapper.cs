using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.MySqlDatabaseWrapper.Configuration;
using Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.Execution;
using Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.ResultTable;

namespace Pustalorc.MySqlDatabaseWrapper.Abstraction;

public abstract class DatabaseConnectorWrapper<TConnectorConfiguration>
    where TConnectorConfiguration : IConnectorConfiguration
{
    [UsedImplicitly] public TConnectorConfiguration Configuration { get; protected set; }

    protected string ConnectionString { get; set; }

    protected DatabaseConnectorWrapper(TConnectorConfiguration configuration,
        DbConnectionStringBuilder connectionStringBuilder)
    {
        Configuration = configuration;
        ConnectionString = connectionStringBuilder.ConnectionString;
    }

    [UsedImplicitly]
    public virtual void ReloadConfiguration(TConnectorConfiguration configuration)
    {
        Configuration = configuration;
        ConnectionString = GetConnectionStringBuilder().ConnectionString;
    }

    [UsedImplicitly]
    public virtual bool TestConnection(out Exception? exception)
    {
        using var connection = GetConnection();
        exception = null;
        try
        {
            connection.Open();
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    protected abstract DbConnectionStringBuilder GetConnectionStringBuilder();

    protected abstract DbConnection GetConnection();

    // Query execution

    [UsedImplicitly]
    public virtual QueryOutput ExecuteQuery(string queryString, EQueryType type = EQueryType.NonQuery,
        Action<QueryOutput, DbConnection, DbTransaction?>? callback = null, params DbParameter[] parameters)
    {
        return ExecuteQuery(new Query(queryString, type, callback, parameters));
    }

    [UsedImplicitly]
    public virtual QueryOutput ExecuteQuery(Query query)
    {
        using var connection = GetConnection();
        connection.Open();

        return ExecuteQueryWithOpenConnection(connection, null, query);
    }

    [UsedImplicitly]
    public virtual QueryOutput ExecuteQueryWithOpenConnection(DbConnection connection, DbTransaction? transaction,
        string queryString, EQueryType type = EQueryType.NonQuery,
        Action<QueryOutput, DbConnection, DbTransaction?>? callback = null, params DbParameter[] parameters)
    {
        return ExecuteQueryWithOpenConnection(connection, transaction,
            new Query(queryString, type, callback, parameters));
    }

    [UsedImplicitly]
    public virtual QueryOutput ExecuteQueryWithOpenConnection(DbConnection connection, DbTransaction? transaction,
        Query query)
    {
        using var command = connection.CreateCommand();

        if (transaction != null)
            command.Transaction = transaction;

        command.CommandText = query.QueryString;
        command.Parameters.AddRange(query.Parameters.ToArray());

        object? output;

        switch (query.Type)
        {
            case EQueryType.NonQuery:
            default:
                output = command.ExecuteNonQuery();
                break;
            case EQueryType.Scalar:
                output = command.ExecuteScalar();
                break;
            case EQueryType.Reader:
                var readerResult = new List<Row>();

                using (var reader = command.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {
                            var columns = new List<Column>();

                            for (var i = 0; i < reader.FieldCount; i++)
                                columns.Add(new Column(reader.GetName(i), reader.GetValue(i)));

                            readerResult.Add(new Row(columns));
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }

                output = readerResult;
                break;
        }

        var constructedOutput = new QueryOutput(query, output);
        query.Callback?.Invoke(constructedOutput, connection, transaction);
        return constructedOutput;
    }

    // Transaction execution

    [UsedImplicitly]
    public virtual List<QueryOutput> ExecuteTransaction(params Query[] queries)
    {
        using var connection = GetConnection();
        connection.Open();

        return ExecuteTransactionWithOpenConnection(connection, queries);
    }

    [UsedImplicitly]
    public virtual List<QueryOutput> ExecuteTransactionWithOpenConnection(DbConnection connection,
        params Query[] queries)
    {
        using var transaction = connection.BeginTransaction();

        try
        {
            var output = queries.Select(query => ExecuteQueryWithOpenConnection(connection, transaction, query))
                .ToList();
            transaction.Commit();
            return output;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}