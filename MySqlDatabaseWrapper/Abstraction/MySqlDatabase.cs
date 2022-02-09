using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Libraries.FrequencyCache;
using Pustalorc.MySqlDatabaseWrapper.Configuration;
using Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.Execution;
using Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.ResultTable;

namespace Pustalorc.MySqlDatabaseWrapper.Abstraction;

public abstract class MySqlDatabaseWrapper<T1, T2> : IDisposable where T1 : IConnectorConfiguration where T2 : class
{
    public T1 Configuration { get; protected set; }

    protected Cache<QueryOutput, T2>? Cache { get; set; }
    protected string ConnectionString { get; set; }
    protected IEqualityComparer<T2> Comparer { get; }

    protected MySqlDatabaseWrapper(T1 configuration, IEqualityComparer<T2> comparer, DbConnectionStringBuilder connectionStringBuilder)
    {
        Comparer = comparer;
        Configuration = configuration;
        ConnectionString = connectionStringBuilder.ConnectionString;

        if (!configuration.UseCache)
            return;

        Cache = new Cache<QueryOutput, T2>(configuration, comparer);
        Cache.OnCachedItemUpdateRequested += CacheItemUpdateRequested;
    }

    public virtual void Dispose()
    {
        Cache?.Dispose();
        GC.SuppressFinalize(this);
    }

    [UsedImplicitly]
    public virtual void ReloadConfiguration(T1 configuration)
    {
        Configuration = configuration;
        ConnectionString = GetConnectionStringBuilder().ConnectionString;

        if (configuration.UseCache)
        {
            Cache?.Dispose();
            Cache = null;
        }

        if (Cache == null)
        {
            Cache = new Cache<QueryOutput, T2>(configuration, Comparer);
            Cache.OnCachedItemUpdateRequested += CacheItemUpdateRequested;
        }
        else
        {
            Cache.ReloadConfiguration(configuration);
        }
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
    public virtual QueryOutput ExecuteQuery(T2 key, Query query, bool checkCache = true)
    {
        if (checkCache)
        {
            var cachedOutput = GetOutputFromCache(key);

            if (cachedOutput != null)
                return cachedOutput;
        }

        using var connection = GetConnection();
        connection.Open();

        var result = ExecuteQueryInternal(connection, query);

        if (Configuration.UseCache && Cache != null && query.ShouldCache)
            Cache.StoreUpdateItem(key, result);

        return result;
    }

    [UsedImplicitly]
    public virtual QueryOutput ExecuteQueryWithOpenConnection(DbConnection connection, T2 key, Query query, DbTransaction? transaction = null, bool checkCache = true)
    {
        if (checkCache)
        {
            var cachedOutput = GetOutputFromCache(key);

            if (cachedOutput != null)
                return cachedOutput;
        }

        var result = ExecuteQueryInternal(connection, query, transaction);

        if (Configuration.UseCache && Cache != null && query.ShouldCache)
            Cache.StoreUpdateItem(key, result);

        return result;
    }

    protected virtual QueryOutput ExecuteQueryInternal(DbConnection connection, Query query, DbTransaction? transaction = null)
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
        query.Callback?.Invoke(constructedOutput, connection);
        return constructedOutput;
    }

    // Transaction execution

    [UsedImplicitly]
    public virtual List<QueryOutput> ExecuteTransaction(params KeyValuePair<T2, Query>[] queries)
    {
        using var connection = GetConnection();
        connection.Open();

        return ExecuteTransactionWithOpenConnection(connection, queries);
    }

    [UsedImplicitly]
    public virtual List<QueryOutput> ExecuteTransactionWithOpenConnection(DbConnection connection, params KeyValuePair<T2, Query>[] queries)
    {
        using var transaction = connection.BeginTransaction();

        try
        {
            var output = (from pair in queries
                let query = pair.Value
                let key = pair.Key
                select ExecuteQueryWithOpenConnection(connection, key, query, transaction)).ToList();

            transaction.Commit();
            return output;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // Cache accessing

    [UsedImplicitly]
    public virtual QueryOutput? GetOutputFromCache(T2 key)
    {
        if (!Configuration.UseCache || Cache == null) return null;

        var cachedItem = Cache.GetItemInCache(key);

        if (cachedItem == null || cachedItem.LastAccess > 9223372036854775806L)
            return null;

        return cachedItem.Item;
    }

    [UsedImplicitly]
    public virtual void RequestCacheUpdate(T2 key, Query query)
    {
        ExecuteQuery(key, query, false);
    }

    [UsedImplicitly]
    public virtual void RequestCacheUpdate(T2 key)
    {
        var cached = Cache?.GetItemInCache(key);
        if (cached == null)
            return;

        RequestCacheUpdate(key, cached.Item.Query);
    }

    private void CacheItemUpdateRequested(AccessCounter<QueryOutput, T2> item)
    {
        RequestCacheUpdate(item.Key, item.Item.Query);
    }
}