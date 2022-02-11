using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Pustalorc.MySqlDatabaseWrapper.Configuration;
using Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.Execution;
using Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.ResultTable;

namespace Pustalorc.MySqlDatabaseWrapper.Abstraction;

/// <summary>
/// A base abstract wrapper for any database connector. Utilizes DbConnection abstract classes and similar to achieve this.
/// </summary>
/// <typeparam name="TConnectorConfiguration">The type for the configuration that will be used. Type must inherit from <see cref="IConnectorConfiguration"/></typeparam>
public abstract class DatabaseConnectorWrapper<TConnectorConfiguration>
    where TConnectorConfiguration : IConnectorConfiguration
{
    /// <summary>
    /// The instance of the configuration passed to the constructor. Necessary for creating the connection string.
    /// </summary>
    [UsedImplicitly]
    public TConnectorConfiguration Configuration { get; protected set; }

    /// <summary>
    /// The connection string to be used for this database. Built by GetConnectionStringBuilder().
    /// </summary>
    protected string ConnectionString { get; set; }

    /// <summary>
    /// Constructor to build the minimum for the connector wrapper.
    /// </summary>
    /// <param name="configuration">The instance of the configuration that we will be using for connecting to the database.</param>
    /// <param name="connectionStringBuilder">The connection string builder so we can get the connection string to use for the database.</param>
    protected DatabaseConnectorWrapper(TConnectorConfiguration configuration,
        DbConnectionStringBuilder connectionStringBuilder)
    {
        Configuration = configuration;
        ConnectionString = connectionStringBuilder.ConnectionString;
    }

    /// <summary>
    /// Reloads the configuration and connection string for the current wrapped connector.
    /// </summary>
    /// <param name="configuration">The new configuration instance to use.</param>
    [UsedImplicitly]
    public virtual void ReloadConfiguration(TConnectorConfiguration configuration)
    {
        Configuration = configuration;
        ConnectionString = GetConnectionStringBuilder().ConnectionString;
    }

    /// <summary>
    /// Tests the connection to see if we can correctly connect to the database from the configuration provided.
    /// </summary>
    /// <param name="exception">The exception, if any, raised in the case of a failed connection.</param>
    /// <returns>
    /// True if the connection was setup successfully, False otherwise.
    /// </returns>
    /// <remarks>
    /// It is recommended to run this method first before running ExecuteQuery, in order to test if the connection can be established.
    /// If it can't, the exception is returned so it can be formatted or reused as necessary.
    /// </remarks>
    [UsedImplicitly]
    public virtual bool TestConnection(out Exception? exception)
    {
        exception = null;
        try
        {
            using var connection = GetConnection();
            connection.Open();
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    /// <summary>
    /// Retrieve the connection string builder for the wrapped connector.
    /// </summary>
    /// <returns>An instance of <see cref="DbConnectionStringBuilder"/> from the wrapped connector.</returns>
    protected abstract DbConnectionStringBuilder GetConnectionStringBuilder();

    /// <summary>
    /// Retrieve the connection for the wrapped connector.
    /// </summary>
    /// <returns>An instance of <see cref="DbConnection"/> from the wrapped connector.</returns>
    protected abstract DbConnection GetConnection();

    // Query execution

    /// <summary>
    /// Executes a query with the provided information.
    /// </summary>
    /// <param name="queryString">The query string to execute.</param>
    /// <param name="type">The type of the query, by default a NonQuery</param>
    /// <param name="callback">A callback to raise after query execution.</param>
    /// <param name="parameters">The parameters to bind to the query before execution.</param>
    /// <returns>An instance of <see cref="QueryOutput"/>, where QueryOutput.Result is the result from the underlying DbConnection Execute methods.</returns>
    /// <remarks>
    /// This method constructs the query and is meant to be used as a quicker way to execute a very precise query without having to run "new Query()" every time.
    /// </remarks>
    [UsedImplicitly]
    public virtual QueryOutput ExecuteQuery(string queryString, EQueryType type = EQueryType.NonQuery,
        Action<QueryOutput, DbConnection, DbTransaction?>? callback = null, params DbParameter[] parameters)
    {
        return ExecuteQuery(new Query(queryString, type, callback, parameters));
    }

    /// <summary>
    /// Executes a specific query.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <returns>An instance of <see cref="QueryOutput"/>, where QueryOutput.Result is the result from the underlying DbConnection Execute methods.</returns>
    /// <remarks>This method will always get and open a new connection. If not intended, please use ExecuteQueryWithOpenConnection instead</remarks>
    [UsedImplicitly]
    public virtual QueryOutput ExecuteQuery(Query query)
    {
        using var connection = GetConnection();
        connection.Open();

        return ExecuteQueryWithOpenConnection(connection, null, query);
    }

    /// <summary>
    /// Executes a query with the provided information and open connection and available transaction (if any).
    /// </summary>
    /// <param name="connection">The open connection to the database.</param>
    /// <param name="transaction">The transaction currently in use by the connection and for the execution of this query.</param>
    /// <param name="queryString">The query string to execute.</param>
    /// <param name="type">The type of the query, by default a NonQuery</param>
    /// <param name="callback">A callback to raise after query execution.</param>
    /// <param name="parameters">The parameters to bind to the query before execution.</param>
    /// <returns>An instance of <see cref="QueryOutput"/>, where QueryOutput.Result is the result from the underlying DbConnection Execute methods.</returns>
    /// <remarks>
    /// This method constructs the query and is meant to be used as a quicker way to execute a very precise query without having to run "new Query()" every time.
    /// The connection provided to this method MUST BE open, otherwise execution will fail.
    /// The transaction by default can be null, but if a transaction is open, it is recommended to pass that here.
    /// </remarks>
    [UsedImplicitly]
    public virtual QueryOutput ExecuteQueryWithOpenConnection(DbConnection connection, DbTransaction? transaction,
        string queryString, EQueryType type = EQueryType.NonQuery,
        Action<QueryOutput, DbConnection, DbTransaction?>? callback = null, params DbParameter[] parameters)
    {
        return ExecuteQueryWithOpenConnection(connection, transaction,
            new Query(queryString, type, callback, parameters));
    }

    /// <summary>
    /// Executes a specific query.
    /// </summary>
    /// <param name="connection">The open connection to the database.</param>
    /// <param name="transaction">The transaction currently in use by the connection and for the execution of this query.</param>
    /// <param name="query">The query to execute.</param>
    /// <returns>An instance of <see cref="QueryOutput"/>, where QueryOutput.Result is the result from the underlying DbConnection Execute methods.</returns>
    /// <remarks>
    /// The connection provided to this method MUST BE open, otherwise execution will fail.
    /// The transaction by default can be null, but if a transaction is open, it is recommended to pass that here.
    /// </remarks>
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

    /// <summary>
    /// Executes a transaction with one or more queries.
    /// </summary>
    /// <param name="queries">All of the queries to execute in this transaction.</param>
    /// <returns>
    /// A list of all the <see cref="QueryOutput"/> from each of the queries in the transaction.
    /// </returns>
    /// <remarks>
    /// To avoid opening a new connection where not necessary, if queries is empty, the method will return an empty result list.
    /// </remarks>
    [UsedImplicitly]
    public virtual List<QueryOutput> ExecuteTransaction(params Query[] queries)
    {
        if (queries.Length == 0)
            return new List<QueryOutput>();

        using var connection = GetConnection();
        connection.Open();

        return ExecuteTransactionWithOpenConnection(connection, queries);
    }

    /// <summary>
    /// Executes a transaction with one or more queries.
    /// </summary>
    /// <param name="connection">The open connection to the database.</param>
    /// <param name="queries">All of the queries to execute in this transaction.</param>
    /// <returns>
    /// A list of all the <see cref="QueryOutput"/> from each of the queries in the transaction.
    /// </returns>
    /// <remarks>
    /// This method will catch and rethrow an exception. This is to force a rollback of the transaction.
    /// The connection provided to this method MUST BE open, otherwise execution will fail.
    /// To avoid starting a new transaction where not necessary, if queries is empty, the method will return an empty result list.
    /// </remarks>
    [UsedImplicitly]
    public virtual List<QueryOutput> ExecuteTransactionWithOpenConnection(DbConnection connection,
        params Query[] queries)
    {
        var output = new List<QueryOutput>();

        if (queries.Length == 0)
            return output;

        using var transaction = connection.BeginTransaction();

        try
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var query in queries)
                output.Add(ExecuteQueryWithOpenConnection(connection, transaction, query));

            transaction.Commit();
            return output;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // Async methods

    // Query execution

    /// <summary>
    /// Executes a query with the provided information.
    /// </summary>
    /// <param name="queryString">The query string to execute.</param>
    /// <param name="type">The type of the query, by default a NonQuery</param>
    /// <param name="callback">A callback to raise after query execution.</param>
    /// <param name="parameters">The parameters to bind to the query before execution.</param>
    /// <returns>An instance of <see cref="QueryOutput"/>, where QueryOutput.Result is the result from the underlying DbConnection Execute methods.</returns>
    /// <remarks>
    /// This method constructs the query and is meant to be used as a quicker way to execute a very precise query without having to run "new Query()" every time.
    /// </remarks>
    [UsedImplicitly]
    public virtual async Task<QueryOutput> ExecuteQueryAsync(string queryString, EQueryType type = EQueryType.NonQuery,
        Action<QueryOutput, DbConnection, DbTransaction?>? callback = null, params DbParameter[] parameters)
    {
        return await ExecuteQueryAsync(new Query(queryString, type, callback, parameters));
    }

    /// <summary>
    /// Executes a specific query.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <returns>An instance of <see cref="QueryOutput"/>, where QueryOutput.Result is the result from the underlying DbConnection Execute methods.</returns>
    /// <remarks>This method will always get and open a new connection. If not intended, please use ExecuteQueryWithOpenConnection instead</remarks>
    [UsedImplicitly]
    public virtual async Task<QueryOutput> ExecuteQueryAsync(Query query)
    {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        await using var connection = GetConnection();
#else
        using var connection = GetConnection();
#endif
        await connection.OpenAsync();

        return await ExecuteQueryWithOpenConnectionAsync(connection, null, query);
    }

    /// <summary>
    /// Executes a query with the provided information and open connection and available transaction (if any).
    /// </summary>
    /// <param name="connection">The open connection to the database.</param>
    /// <param name="transaction">The transaction currently in use by the connection and for the execution of this query.</param>
    /// <param name="queryString">The query string to execute.</param>
    /// <param name="type">The type of the query, by default a NonQuery</param>
    /// <param name="callback">A callback to raise after query execution.</param>
    /// <param name="parameters">The parameters to bind to the query before execution.</param>
    /// <returns>An instance of <see cref="QueryOutput"/>, where QueryOutput.Result is the result from the underlying DbConnection Execute methods.</returns>
    /// <remarks>
    /// This method constructs the query and is meant to be used as a quicker way to execute a very precise query without having to run "new Query()" every time.
    /// The connection provided to this method MUST BE open, otherwise execution will fail.
    /// The transaction by default can be null, but if a transaction is open, it is recommended to pass that here.
    /// </remarks>
    [UsedImplicitly]
    public virtual async Task<QueryOutput> ExecuteQueryWithOpenConnectionAsync(DbConnection connection,
        DbTransaction? transaction,
        string queryString, EQueryType type = EQueryType.NonQuery,
        Action<QueryOutput, DbConnection, DbTransaction?>? callback = null, params DbParameter[] parameters)
    {
        return await ExecuteQueryWithOpenConnectionAsync(connection, transaction,
            new Query(queryString, type, callback, parameters));
    }

    /// <summary>
    /// Executes a specific query.
    /// </summary>
    /// <param name="connection">The open connection to the database.</param>
    /// <param name="transaction">The transaction currently in use by the connection and for the execution of this query.</param>
    /// <param name="query">The query to execute.</param>
    /// <returns>An instance of <see cref="QueryOutput"/>, where QueryOutput.Result is the result from the underlying DbConnection Execute methods.</returns>
    /// <remarks>
    /// The connection provided to this method MUST BE open, otherwise execution will fail.
    /// The transaction by default can be null, but if a transaction is open, it is recommended to pass that here.
    /// </remarks>
    [UsedImplicitly]
    public virtual async Task<QueryOutput> ExecuteQueryWithOpenConnectionAsync(DbConnection connection,
        DbTransaction? transaction, Query query)
    {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        await using var command = connection.CreateCommand();
#else
        using var command = connection.CreateCommand();
#endif

        if (transaction != null)
            command.Transaction = transaction;

        command.CommandText = query.QueryString;
        command.Parameters.AddRange(query.Parameters.ToArray());

        object? output;

        switch (query.Type)
        {
            case EQueryType.NonQuery:
            default:
                output = await command.ExecuteNonQueryAsync();
                break;
            case EQueryType.Scalar:
                output = await command.ExecuteScalarAsync();
                break;
            case EQueryType.Reader:
                var readerResult = new List<Row>();

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
                await using (var reader = await command.ExecuteReaderAsync())
#else
                using (var reader = await command.ExecuteReaderAsync())
#endif
                {
                    try
                    {
                        while (await reader.ReadAsync())
                        {
                            var columns = new List<Column>();

                            for (var i = 0; i < reader.FieldCount; i++)
                                columns.Add(new Column(reader.GetName(i), reader.GetValue(i)));

                            readerResult.Add(new Row(columns));
                        }
                    }
                    finally
                    {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
                        await reader.CloseAsync();
#else
                        reader.Close();
#endif
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

    /// <summary>
    /// Executes a transaction with one or more queries.
    /// </summary>
    /// <param name="queries">All of the queries to execute in this transaction.</param>
    /// <returns>
    /// A list of all the <see cref="QueryOutput"/> from each of the queries in the transaction.
    /// </returns>
    /// <remarks>
    /// To avoid opening a new connection where not necessary, if queries is empty, the method will return an empty result list.
    /// </remarks>
    [UsedImplicitly]
    public virtual async Task<List<QueryOutput>> ExecuteTransactionAsync(params Query[] queries)
    {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        await using var connection = GetConnection();
#else
        using var connection = GetConnection();
#endif
        await connection.OpenAsync();

        return await ExecuteTransactionWithOpenConnectionAsync(connection, queries);
    }

    /// <summary>
    /// Executes a transaction with one or more queries.
    /// </summary>
    /// <param name="connection">The open connection to the database.</param>
    /// <param name="queries">All of the queries to execute in this transaction.</param>
    /// <returns>
    /// A list of all the <see cref="QueryOutput"/> from each of the queries in the transaction.
    /// </returns>
    /// <remarks>
    /// This method will catch and rethrow an exception. This is to force a rollback of the transaction.
    /// The connection provided to this method MUST BE open, otherwise execution will fail.
    /// To avoid starting a new transaction where not necessary, if queries is empty, the method will return an empty result list.
    /// </remarks>
    [UsedImplicitly]
    public virtual async Task<List<QueryOutput>> ExecuteTransactionWithOpenConnectionAsync(DbConnection connection,
        params Query[] queries)
    {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        await using var transaction = await connection.BeginTransactionAsync();
#else
        using var transaction = connection.BeginTransaction();
#endif

        try
        {
            var output = new List<QueryOutput>();

            foreach (var query in queries)
                output.Add(await ExecuteQueryWithOpenConnectionAsync(connection, transaction, query));

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            await transaction.CommitAsync();
#else
            transaction.Commit();
#endif
            return output;
        }
        catch
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            await transaction.RollbackAsync();
#else
            transaction.Rollback();
#endif
            throw;
        }
    }
}