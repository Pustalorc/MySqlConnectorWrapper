using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
using Pustalorc.Libraries.FrequencyCache;
using Pustalorc.MySql.Data.Wrapper.Configuration;
using Pustalorc.MySql.Data.Wrapper.Queries;
using Pustalorc.MySql.Data.Wrapper.TableStructure;

namespace Pustalorc.MySql.Data.Wrapper
{
    /// <summary>
    /// The connector. Inherit it and pass a configuration class to it.
    /// </summary>
    /// <typeparam name="T">The type, which inherits from IConnectorConfiguration, which should be used by the connector.</typeparam>
    public abstract class ConnectorWrapper<T> where T : IConnectorConfiguration
    {
        /// <summary>
        /// The caching system that the connector should use.
        /// </summary>
        private readonly CacheManager<QueryOutput> m_CacheManager;

        /// <summary>
        /// The original unmodified passed configuration to the class.
        /// </summary>
        protected internal readonly T Configuration;

        /// <summary>
        /// Default constructor, only requires an instance of type T to be used as main configuration.
        /// This already tests if the connection can be opened or not.
        /// </summary>
        /// <param name="configuration">The instance of type T to be used as main configuration</param>
        protected ConnectorWrapper(T configuration)
        {
            Configuration = configuration;

            if (configuration.UseCache)
            {
                m_CacheManager = new CacheManager<QueryOutput>(configuration);
                m_CacheManager.OnCachedItemUpdateRequested += CacheItemUpdateRequested;
            }

            using (var connection = CreateConnection())
            {
                try
                {
                    connection.Open();
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Creates the connection to the MySql Database
        /// </summary>
        /// <returns>A MySqlConnection object if it succeeded at creating one from the connection string, null otherwise.</returns>
        private MySqlConnection CreateConnection()
        {
            return new MySqlConnection(
                $"SERVER={Configuration.DatabaseAddress};DATABASE={Configuration.DatabaseName};" +
                $"UID={MySqlUtilities.ToSafeValue(Configuration.DatabaseUsername)};PASSWORD={MySqlUtilities.ToSafeValue(Configuration.DatabasePassword)};" +
                $"PORT={Configuration.DatabasePort};{Configuration.ConnectionStringExtras}");
        }

        /// <summary>
        /// Executes a single query asynchronously.
        /// </summary>
        /// <param name="query">The query to be executed.</param>
        /// <returns>The result of the query executed.</returns>
        public async Task<QueryOutput> ExecuteQueryAsync(Query query)
        {
            var output = GetOutputFromCache(query);
            if (output != null)
            {
                RaiseCallbacks(query.Callbacks, output);
                return output;
            }

            using (var connection = CreateConnection())
            {
                try
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        return await RunCommandAsync(query, command);
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Executes a single query synchronously.
        /// </summary>
        /// <param name="query">The query to be executed.</param>
        /// <returns>The result of the query executed.</returns>
        public QueryOutput ExecuteQuery(Query query)
        {
            var output = GetOutputFromCache(query);
            if (output != null)
            {
                RaiseCallbacks(query.Callbacks, output);
                return output;
            }

            using (var connection = CreateConnection())
            {
                try
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        return RunCommand(query, command);
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Executes a group of queries asynchronously.
        /// If an error is encountered, everything is rolled back and the exception is re-thrown.
        /// </summary>
        /// <param name="queries">The group of queries to be executed.</param>
        /// <returns>The result of the group of queries executed.</returns>
        public async Task<IEnumerable<QueryOutput>> ExecuteTransactionAsync(params Query[] queries)
        {
            var result = new List<QueryOutput>();

            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        using (var command = connection.CreateCommand())
                        {
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
                                            await connection.OpenAsync();
                                            break;
                                        case ConnectionState.Broken:
                                            await connection.CloseAsync();
                                            await connection.OpenAsync();
                                            break;
                                    }

                                    QueryOutput queryOutput;
                                    try
                                    {
                                        queryOutput = await RunCommandAsync(query, command);
                                    }
                                    catch (MySqlException ex)
                                    {
                                        if (ex.Message.Equals(Resources.Timeout, StringComparison.OrdinalIgnoreCase))
                                            queryOutput = await RunCommandAsync(query, command);
                                        else
                                            throw;
                                    }

                                    result.Add(queryOutput);
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                    finally
                    {
                        await connection.CloseAsync();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Executes a group of queries synchronously.
        /// If an error is encountered, everything is rolled back and the exception is re-thrown.
        /// </summary>
        /// <param name="queries">The group of queries to be executed.</param>
        /// <returns>The result of the group of queries executed.</returns>
        public IEnumerable<QueryOutput> ExecuteTransaction(params Query[] queries)
        {
            var result = new List<QueryOutput>();

            using (var connection = CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var command = connection.CreateCommand())
                        {
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
                                            connection.Open();
                                            break;
                                        case ConnectionState.Broken:
                                            connection.Close();
                                            connection.Open();
                                            break;
                                    }

                                    QueryOutput queryOutput;
                                    try
                                    {
                                        queryOutput = RunCommand(query, command);
                                    }
                                    catch (MySqlException ex)
                                    {
                                        if (ex.Message.Equals(Resources.Timeout, StringComparison.OrdinalIgnoreCase))
                                            queryOutput = RunCommand(query, command);
                                        else
                                            throw;
                                    }

                                    result.Add(queryOutput);
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Requests and updates the cache of the specified query.
        /// There is no output, as this is just a request.
        /// </summary>
        /// <param name="query">The query to update in the cache.</param>
        public async Task RequestCacheUpdateAsync(Query query)
        {
            using (var connection = CreateConnection())
            {
                try
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        await RunCommandAsync(query, command);
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Requests and updates the cache of the specified query.
        /// There is no output, as this is just a request.
        /// </summary>
        /// <param name="query">The query to update in the cache.</param>
        public void RequestCacheUpdate(Query query)
        {
            using (var connection = CreateConnection())
            {
                try
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        RunCommand(query, command);
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Updates the cache's refresh timer with a new interval.
        /// </summary>
        /// <param name="rate">The new rate (in ms) that the cache should be refreshed at.</param>
        protected void ModifyCacheRefreshInterval(double rate)
        {
            if (!Configuration.UseCache) return;

            m_CacheManager.ModifyCacheRefreshInterval(rate);
        }

        /// <summary>
        /// Runs the selected query in the context of the specified Command asynchronously.
        /// </summary>
        /// <param name="query">The query to be ran.</param>
        /// <param name="command">The command context to be used.</param>
        /// <returns>The output after the query gets executed.</returns>
        private async Task<QueryOutput> RunCommandAsync(Query query, MySqlCommand command)
        {
            var queryOutput = new QueryOutput(query, null);

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

                    using (var reader = await command.ExecuteReaderAsync())
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
                            reader.Close();
                        }
                    }

                    queryOutput.Output = readerResult;
                    break;
            }

            RaiseCallbacks(query.Callbacks, queryOutput);

            if (Configuration.UseCache && query.ShouldCache)
                m_CacheManager.StoreUpdateItem(queryOutput);

            return queryOutput;
        }

        /// <summary>
        /// Runs the selected query in the context of the specified Command synchronously.
        /// </summary>
        /// <param name="query">The query to be ran.</param>
        /// <param name="command">The command context to be used.</param>
        /// <returns>The output after the query gets executed.</returns>
        private QueryOutput RunCommand(Query query, MySqlCommand command)
        {
            var queryOutput = new QueryOutput(query, null);

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
                m_CacheManager.StoreUpdateItem(queryOutput);

            return queryOutput;
        }

        /// <summary>
        /// Retrieves a previously stored output of the query from cache.
        /// </summary>
        /// <param name="query">The query with the unique identifier related to an element in cache.</param>
        /// <returns>Null if there is no output in the cache for this query, otherwise its a valid instance of the QueryOutput object.</returns>
        private QueryOutput GetOutputFromCache(Query query)
        {
            if (!Configuration.UseCache || !query.ShouldCache) return null;

            var cache = m_CacheManager.GetItemInCache(new QueryOutput(query, null));

            return cache?.Identifiable;
        }

        /// <summary>
        /// Raises the callbacks provided with an output.
        /// </summary>
        /// <param name="callbacks">The callbacks to be raised.</param>
        /// <param name="output">The output to be passed to all the callbacks.</param>
        private static void RaiseCallbacks(IEnumerable<QueryCallback> callbacks, QueryOutput output)
        {
            foreach (var callback in callbacks)
                callback?.Invoke(output);
        }

        /// <summary>
        /// Deals with the event raised by the cache to update the requested element. A single access to the identifiable is needed.
        /// </summary>
        /// <param name="item">The element in cache to update.</param>
        /// <param name="identifiable">The inner identifiable of the previous item.</param>
        private void CacheItemUpdateRequested(CachedItem<QueryOutput> item, ref QueryOutput identifiable)
        {
            RequestCacheUpdate(identifiable.Query);
        }
    }
}