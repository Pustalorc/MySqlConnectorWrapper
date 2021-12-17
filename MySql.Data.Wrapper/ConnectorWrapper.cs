using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MySql.Data.MySqlClient;
using Pustalorc.Libraries.FrequencyCache;
using Pustalorc.MySql.Data.Wrapper.Configuration;
using Pustalorc.MySql.Data.Wrapper.Queries;
using Pustalorc.MySql.Data.Wrapper.TableStructure;

namespace Pustalorc.MySql.Data.Wrapper;

/// <summary>
/// The connector. Inherit it and pass a configuration class to it.
/// </summary>
/// <typeparam name="T1">The type, which inherits from IConnectorConfiguration, which should be used by the connector as configuration.</typeparam>
/// <typeparam name="T2">The type which is used as a key identifier in cache.</typeparam>
[UsedImplicitly]
public class ConnectorWrapper<T1, T2> where T1 : IConnectorConfiguration where T2 : class
{
    /// <summary>
    /// The caching system that the connector should use.
    /// </summary>
    protected readonly Cache<QueryOutput<T2>, T2> Cache;

    /// <summary>
    /// The original unmodified passed configuration to the class.
    /// </summary>
    protected readonly T1 Configuration;

    /// <summary>
    /// The connection currently in use.
    /// </summary>
    protected readonly ThreadLocal<MySqlConnection> Connection;

    /// <summary>
    /// Default constructor, only requires an instance of type T to be used as main configuration.
    /// </summary>
    /// <param name="configuration">The instance of type T to be used as main configuration.</param>
    /// <param name="comparer">An <see cref="IEqualityComparer{T2}"/> that defines how the key type will be compared in cache.</param>
    /// <remarks>
    /// Testing if the connector can connect to the MySql server is recommended. Use <see cref="TestConnection"/> for such.
    /// </remarks>
    public ConnectorWrapper(T1 configuration, IEqualityComparer<T2> comparer)
    {
        Configuration = configuration;
        Connection = new ThreadLocal<MySqlConnection>();

        if (!configuration.UseCache)
        {
            Cache = null!;
            return;
        }

        Cache = new Cache<QueryOutput<T2>, T2>(configuration, comparer);
        Cache.OnCachedItemUpdateRequested += CacheItemUpdateRequested;
    }

    /// <summary>
    /// Creates a new query from inputs.
    /// </summary>
    /// <param name="key">The key to identify this query in cache.</param>
    /// <param name="query">The query string to run.</param>
    /// <param name="type">The type of query we are running.</param>
    /// <param name="shouldCache">If the query should be cached at all.</param>
    /// <param name="parameters">The parameters that will be used on the query string.</param>
    /// <param name="callbacks">Any callbacks to be raised after the query runs.</param>
    /// <returns>
    /// Returns a new <see cref="Query{T2}"/> instance.
    /// </returns>
    [UsedImplicitly]
    public Query<T2> CreateQuery(T2 key, string query, EQueryType type, bool shouldCache = false,
        IEnumerable<MySqlParameter>? parameters = null, IEnumerable<Query<T2>.QueryCallback>? callbacks = null)
    {
        return new Query<T2>(key, query, type, shouldCache, parameters, callbacks);
    }

    /// <summary>
    /// Tests the connection. Returns false if the connection throws any exception.
    /// </summary>
    /// <param name="exception">Outs null if no exception was raised, otherwise returns the same exception that was raised.</param>
    /// <returns>
    /// True if the connection succeeded.
    /// False if the connection failed.
    /// </returns>
    [UsedImplicitly]
    public virtual bool TestConnection(out Exception? exception)
    {
        var connection = GetCreateConnection();
        exception = null;
        try
        {
            SafeOpenConnection(connection);
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
        finally
        {
            SafeCloseConnection(connection);
        }
    }

    /// <summary>
    /// Opens a connection without throwing an error if the connection is already open.
    /// </summary>
    /// <param name="connection">The connection to open.</param>
    /// <remarks>
    /// If the connection is broken or fails entirely, this should still throw an exception.
    /// </remarks>
    protected virtual void SafeOpenConnection(MySqlConnection connection)
    {
        if (connection.State != ConnectionState.Open)
            connection.Open();
    }

    /// <summary>
    /// Closes a connection without throwing an error if the connection is already closed.
    /// </summary>
    /// <param name="connection">The connection to close.</param>
    /// <remarks>
    /// If the connection is broken or fails entirely, this should still throw an exception.
    /// </remarks>
    protected virtual void SafeCloseConnection(MySqlConnection connection)
    {
        if (connection.State != ConnectionState.Closed)
            connection.Close();
    }

    /// <summary>
    /// Opens a connection without throwing an error if the connection is already open.
    /// </summary>
    /// <param name="connection">The connection to open.</param>
    /// <remarks>
    /// If the connection is broken or fails entirely, this should still throw an exception.
    /// </remarks>
    protected virtual async Task SafeOpenConnectionAsync(MySqlConnection connection)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
    }

    /// <summary>
    /// Closes a connection without throwing an error if the connection is already closed.
    /// </summary>
    /// <param name="connection">The connection to close.</param>
    /// <remarks>
    /// If the connection is broken or fails entirely, this should still throw an exception.
    /// </remarks>
    protected virtual async Task SafeCloseConnectionAsync(MySqlConnection connection)
    {
        if (connection.State != ConnectionState.Closed)
            await connection.CloseAsync();
    }

    /// <summary>
    /// Gets or creates a new connection.
    /// </summary>
    /// <returns>A new MySqlConnection for the current thread.</returns>
    [UsedImplicitly]
    protected virtual MySqlConnection GetCreateConnection()
    {
        if (!Connection.IsValueCreated || Connection.Value == null)
            Connection.Value = new MySqlConnection(string.Format(Configuration.ConnectionStringFormat,
                Configuration.DatabaseAddress, Configuration.DatabaseName,
                MySqlUtilities.ToSafeConnectionStringValue(Configuration.DatabaseUsername),
                MySqlUtilities.ToSafeConnectionStringValue(Configuration.DatabasePassword),
                Configuration.DatabasePort));

        return Connection.Value;
    }

    /// <summary>
    /// Executes a single query asynchronously.
    /// </summary>
    /// <param name="query">The query to be executed.</param>
    /// <returns>The result of the query executed.</returns>
    [UsedImplicitly]
    public virtual async Task<QueryOutput<T2>> ExecuteQueryAsync(Query<T2> query)
    {
        var output = GetOutputFromCache(query);
        if (output != null)
        {
            RaiseCallbacks(query.Callbacks, output);
            return output;
        }

        var connection = GetCreateConnection();
        try
        {
            await SafeOpenConnectionAsync(connection);

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            await using var command = connection.CreateCommand();
#else
            using var command = connection.CreateCommand();
#endif
            return await RunCommandAsync(query, command);
        }
        finally
        {
            await SafeCloseConnectionAsync(connection);
        }
    }

    /// <summary>
    /// Executes a single query synchronously.
    /// </summary>
    /// <param name="query">The query to be executed.</param>
    /// <returns>The result of the query executed.</returns>
    [UsedImplicitly]
    public virtual QueryOutput<T2> ExecuteQuery(Query<T2> query)
    {
        var output = GetOutputFromCache(query);
        if (output != null)
        {
            RaiseCallbacks(query.Callbacks, output);
            return output;
        }

        var connection = GetCreateConnection();
        try
        {
            SafeOpenConnection(connection);
            using var command = connection.CreateCommand();
            return RunCommand(query, command);
        }
        finally
        {
            SafeCloseConnection(connection);
        }
    }

    /// <summary>
    /// Executes a group of queries asynchronously.
    /// If an error is encountered, everything is rolled back and the exception is re-thrown.
    /// </summary>
    /// <param name="queries">The group of queries to be executed.</param>
    /// <returns>The result of the group of queries executed.</returns>
    [UsedImplicitly]
    public virtual async Task<IEnumerable<QueryOutput<T2>>> ExecuteTransactionAsync(params Query<T2>[] queries)
    {
        var result = new List<QueryOutput<T2>>();

        var connection = GetCreateConnection();
        await SafeOpenConnectionAsync(connection);
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        await using var transaction = await connection.BeginTransactionAsync();
#else
        using var transaction = await connection.BeginTransactionAsync();
#endif
        try
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            await using (var command = connection.CreateCommand())
#else
            using (var command = connection.CreateCommand())
#endif
            {
                command.Transaction = transaction;
                foreach (var query in queries)
                {
                    var output = GetOutputFromCache(query);
                    if (output != null)
                    {
                        RaiseCallbacks(query.Callbacks, output);
                        result.Add(output);
                    }
                    else
                    {
                        switch (connection.State)
                        {
                            case ConnectionState.Closed:
                                await SafeOpenConnectionAsync(connection);
                                break;
                            case ConnectionState.Broken:
                                await SafeCloseConnectionAsync(connection);
                                await SafeOpenConnectionAsync(connection);
                                break;
                        }

                        var queryOutput = await RunCommandAsync(query, command);
                        result.Add(queryOutput);
                    }
                }
            }

            await SafeOpenConnectionAsync(connection);
            transaction.Commit();
        }
        catch (Exception)
        {
            await SafeOpenConnectionAsync(connection);
            transaction.Rollback();
            throw;
        }
        finally
        {
            await SafeCloseConnectionAsync(connection);
        }

        return result;
    }

    /// <summary>
    /// Executes a group of queries synchronously.
    /// If an error is encountered, everything is rolled back and the exception is re-thrown.
    /// </summary>
    /// <param name="queries">The group of queries to be executed.</param>
    /// <returns>The result of the group of queries executed.</returns>
    [UsedImplicitly]
    public virtual IEnumerable<QueryOutput<T2>> ExecuteTransaction(params Query<T2>[] queries)
    {
        var result = new List<QueryOutput<T2>>();

        var connection = GetCreateConnection();
        SafeOpenConnection(connection);
        using var transaction = connection.BeginTransaction();
        try
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;

                foreach (var query in queries)
                {
                    var output = GetOutputFromCache(query);
                    if (output != null)
                    {
                        RaiseCallbacks(query.Callbacks, output);
                        result.Add(output);
                    }
                    else
                    {
                        switch (connection.State)
                        {
                            case ConnectionState.Closed:
                                SafeOpenConnection(connection);
                                break;
                            case ConnectionState.Broken:
                                SafeCloseConnection(connection);
                                SafeOpenConnection(connection);
                                break;
                        }

                        var queryOutput = RunCommand(query, command);
                        result.Add(queryOutput);
                    }
                }
            }

            SafeOpenConnection(connection);
            transaction.Commit();
        }
        catch (Exception)
        {
            SafeOpenConnection(connection);
            transaction.Rollback();
            throw;
        }
        finally
        {
            SafeCloseConnection(connection);
        }

        return result;
    }

    /// <summary>
    /// Requests and updates the cache of the specified query.
    /// There is no output, as this is just a request.
    /// </summary>
    /// <param name="query">The query to update in the cache.</param>
    [UsedImplicitly]
    public virtual async Task RequestCacheUpdateAsync(Query<T2> query)
    {
        var connection = GetCreateConnection();
        try
        {
            await SafeOpenConnectionAsync(connection);

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            await using var command = connection.CreateCommand();
#else
            using var command = connection.CreateCommand();
#endif
            await RunCommandAsync(query, command);
        }
        finally
        {
            await SafeCloseConnectionAsync(connection);
        }
    }

    /// <summary>
    /// Requests and updates the cache of the specified query.
    /// There is no output, as this is just a request.
    /// </summary>
    /// <param name="query">The query to update in the cache.</param>
    [UsedImplicitly]
    public virtual void RequestCacheUpdate(Query<T2> query)
    {
        var connection = GetCreateConnection();
        try
        {
            SafeOpenConnection(connection);
            using var command = connection.CreateCommand();
            RunCommand(query, command);
        }
        finally
        {
            SafeCloseConnection(connection);
        }
    }

    /// <summary>
    /// Updates the cache's refresh timer with a new interval.
    /// </summary>
    /// <param name="rate">The new rate (in ms) that the cache should be refreshed at.</param>
    [UsedImplicitly]
    public virtual void ModifyCacheRefreshInterval(double rate)
    {
        if (!Configuration.UseCache) return;

        Cache.ModifyCacheRefreshInterval(rate);
    }

    /// <summary>
    /// Runs the selected query in the context of the specified Command asynchronously.
    /// </summary>
    /// <param name="query">The query to be ran.</param>
    /// <param name="command">The command context to be used.</param>
    /// <returns>The output after the query gets executed.</returns>
    protected virtual async Task<QueryOutput<T2>> RunCommandAsync(Query<T2> query, MySqlCommand command)
    {
        var queryOutput = new QueryOutput<T2>(query, null);

        command.CommandText = query.QueryString;
        command.Parameters.Clear();
        command.Parameters.AddRange(query.Parameters.ToArray());

        switch (query.Type)
        {
            case EQueryType.NonQuery:
                queryOutput.Output = await command.ExecuteNonQueryAsync();
                break;
            case EQueryType.Scalar:
                queryOutput.Output = await command.ExecuteScalarAsync();
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

                queryOutput.Output = readerResult;
                break;
        }

        RaiseCallbacks(query.Callbacks, queryOutput);

        if (Configuration.UseCache && query.ShouldCache)
            Cache.StoreUpdateItem(queryOutput.Query.Key, queryOutput);

        return queryOutput;
    }

    /// <summary>
    /// Runs the selected query in the context of the specified Command synchronously.
    /// </summary>
    /// <param name="query">The query to be ran.</param>
    /// <param name="command">The command context to be used.</param>
    /// <returns>The output after the query gets executed.</returns>
    protected virtual QueryOutput<T2> RunCommand(Query<T2> query, MySqlCommand command)
    {
        var queryOutput = new QueryOutput<T2>(query, null);

        command.CommandText = query.QueryString;
        command.Parameters.Clear();
        command.Parameters.AddRange(query.Parameters.ToArray());

        switch (query.Type)
        {
            case EQueryType.NonQuery:
                queryOutput.Output = command.ExecuteNonQuery();
                break;
            case EQueryType.Scalar:
                queryOutput.Output = command.ExecuteScalar();
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

                queryOutput.Output = readerResult;
                break;
        }

        RaiseCallbacks(query.Callbacks, queryOutput);

        if (Configuration.UseCache && query.ShouldCache)
            Cache.StoreUpdateItem(queryOutput.Query.Key, queryOutput);

        return queryOutput;
    }

    /// <summary>
    /// Retrieves a previously stored output of the query from cache.
    /// </summary>
    /// <param name="query">The query with the unique identifier related to an element in cache.</param>
    /// <returns>Null if there is no output in the cache for this query, otherwise its a valid instance of the QueryOutput object.</returns>
    [UsedImplicitly]
    public virtual QueryOutput<T2>? GetOutputFromCache(Query<T2> query)
    {
        return !query.ShouldCache ? null : GetOutputFromCache(query.Key);
    }

    /// <summary>
    /// Retrieves a previously stored output of the query from cache.
    /// </summary>
    /// <param name="key">The key of an element in cache.</param>
    /// <returns>Null if there is no output in the cache for this query, otherwise its a valid instance of the QueryOutput object.</returns>
    [UsedImplicitly]
    public virtual QueryOutput<T2>? GetOutputFromCache(T2 key)
    {
        if (!Configuration.UseCache) return null;

        var cache = Cache.GetItemInCache(key);

        return cache?.Item;
    }

    /// <summary>
    /// Raises the callbacks provided with an output.
    /// </summary>
    /// <param name="callbacks">The callbacks to be raised.</param>
    /// <param name="output">The output to be passed to all the callbacks.</param>
    protected virtual void RaiseCallbacks(IEnumerable<Query<T2>.QueryCallback> callbacks, QueryOutput<T2> output)
    {
        foreach (var callback in callbacks)
            callback.Invoke(output);
    }

    /// <summary>
    /// Deals with the event raised by the cache to update the requested element. A single access to the identifiable is needed.
    /// </summary>
    /// <param name="item">The element in cache to update.</param>
    private void CacheItemUpdateRequested(AccessCounter<QueryOutput<T2>> item)
    {
        RequestCacheUpdate(item.Item.Query);
    }
}