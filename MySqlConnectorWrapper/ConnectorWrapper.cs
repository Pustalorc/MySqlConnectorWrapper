using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using Pustalorc.Libraries.MySqlConnectorWrapper.Caching;
using Pustalorc.Libraries.MySqlConnectorWrapper.Configuration;
using Pustalorc.Libraries.MySqlConnectorWrapper.Queries;
using Pustalorc.Libraries.MySqlConnectorWrapper.Queueing;
using Pustalorc.Libraries.MySqlConnectorWrapper.TableStructure;

namespace Pustalorc.Libraries.MySqlConnectorWrapper
{
    /// <summary>
    ///     The connector. Inherit it and pass a configuration class to it.
    /// </summary>
    /// <typeparam name="T">The type, which inherits from IConnectorConfiguration, which should be used by the connector.</typeparam>
    public abstract class ConnectorWrapper<T> : IDisposable where T : IConnectorConfiguration
    {
        /// <summary>
        ///     The queue that the connector should use.
        /// </summary>
        private readonly ConnectorQueue<T> _connectorQueue;

        /// <summary>
        ///     The caching system that the connector should use.
        /// </summary>
        private readonly CacheManager<T> _cacheManager;

        /// <summary>
        ///     The original unmodified passed configuration to the class.
        /// </summary>
        protected internal readonly T Configuration;

        /// <summary>
        ///     Default constructor, only requires an instance of type T to be used as main configuration.
        ///     This already tests if the connection can be opened or not.
        /// </summary>
        /// <param name="configuration">The instance of type T to be used as main configuration</param>
        protected ConnectorWrapper(T configuration)
        {
            Configuration = configuration;

            if (configuration.UseCache) _cacheManager = new CacheManager<T>(this);

            _connectorQueue = new ConnectorQueue<T>(this);

            using (var connection = CreateConnection())
            {
                try
                {
                    connection.Open();
                }
                catch (MySqlException ex)
                {
                    Utils.LogConsole("MySqlConnectorWrapper.Constructor",
                        ex.Number == 1042 ? "Can't connect to MySQL host." : ex.Message);

                    throw;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        ///     Creates the connection to the MySql Database
        /// </summary>
        /// <returns>A MySqlConnection object if it succeeded at creating one from the connection string, null otherwise.</returns>
        private MySqlConnection CreateConnection()
        {
            MySqlConnection connection = null;

            try
            {
                connection = new MySqlConnection(
                    $"SERVER={Configuration.DatabaseAddress};DATABASE={Configuration.DatabaseName};UID={Utils.ToSafeValue(Configuration.DatabaseUsername)};PASSWORD={Utils.ToSafeValue(Configuration.DatabasePassword)};PORT={Configuration.DatabasePort};");
            }
            catch (Exception ex)
            {
                Utils.LogConsole("MySqlConnectorWrapper.CreateConnection", ex.Message);
            }

            return connection;
        }

        /// <summary>
        ///     Requests to execute a list of queries.
        /// </summary>
        /// <param name="isTransaction">Defines if the queries should be executed within the same MySql Transaction</param>
        /// <param name="queries">The queries to be executed.</param>
        public void RequestQueryExecute(bool isTransaction, params Query[] queries)
        {
            foreach (var query in queries)
            {
                if (Configuration.UseCache && query.ShouldCache && query.QueryCallback != null)
                {
                    var cache = _cacheManager.GetItemInCache(query);
                    if (cache != null)
                    {
                        query.QueryCallback.Invoke(cache);
                        continue;
                    }
                }

                if (!isTransaction) _connectorQueue.Enqueue(query);
            }

            if (isTransaction)
                _connectorQueue.EnqueueTransaction(queries);
        }

        /// <summary>
        ///     Executes a single query. If called directly from inherited class, it ignores queueing.
        /// </summary>
        /// <param name="query">The query to be executed.</param>
        /// <returns>The result of the query executed.</returns>
        public QueryOutput ExecuteQuery(Query query)
        {
            using (var connection = CreateConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    try
                    {
                        return RunCommand(query, command);
                    }
                    catch (Exception ex)
                    {
                        Utils.LogConsole("MySqlConnectorWrapper.ExecuteQuery",
                            $"Query \"{query.QueryString}\" threw:\n{ex.Message}");
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

            return new QueryOutput(query, null);
        }

        /// <summary>
        ///     Executes a group of queries. If called directly from inherited class, it ignores queueing.
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
                using (var command = connection.CreateCommand())
                {
                    try
                    {
                        result.AddRange(queries.Select(query => RunCommand(query, command)));
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch (Exception e)
                        {
                            Utils.LogConsole("MySqlConnectorWrapper.ExecuteTransaction",
                                $"Exception happened during rollback:\n{e.Message}");
                        }

                        Utils.LogConsole("MySqlConnectorWrapper.ExecuteTransaction",
                            $"Exception happened during commit:\n{ex.Message}");
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
        ///     Runs the selected query in the context of the specified Command.
        /// </summary>
        /// <param name="query">The query to be ran.</param>
        /// <param name="command">The command context to be used.</param>
        /// <returns>The output after the query gets executed.</returns>
        private QueryOutput RunCommand(Query query, MySqlCommand command)
        {
            var queryOutput = new QueryOutput(query, null);

            try
            {
                command.CommandText = query.QueryString;
                command.Parameters.Clear();

                foreach (var param in query.QueryParameters)
                    command.Parameters.Add(param);

                switch (query.QueryType)
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
                                    try
                                    {
                                        var columns = new List<Column>();

                                        for (var i = 0; i < reader.FieldCount; i++)
                                            columns.Add(new Column(reader.GetName(i),
                                                reader.GetValue(i)));

                                        readerResult.Add(new Row(columns));
                                    }
                                    catch (Exception ex)
                                    {
                                        Utils.LogConsole("MySqlConnectorWrapper.Reader",
                                            $"[During READ] Query \"{query.QueryString}\" threw:\n{ex.Message}");
                                    }
                            }
                            catch (Exception ex)
                            {
                                Utils.LogConsole("MySqlConnectorWrapper.Reader",
                                    $"Query \"{query.QueryString}\" threw:\n{ex.Message}");
                            }
                            finally
                            {
                                reader.Close();
                            }
                        }

                        queryOutput.Output = readerResult;
                        break;
                }

                try
                {
                    query.QueryCallback?.Invoke(queryOutput);
                }
                catch (Exception ex)
                {
                    Utils.LogConsole("MySqlConnectorWrapper.QueryCallback", $"Query \"{query.QueryString}\" threw during callback:\n{ex.Message}");
                }

                if (Configuration.UseCache && query.ShouldCache)
                    _cacheManager.StoreUpdateItemInCache(queryOutput);
            }
            catch (Exception ex)
            {
                Utils.LogConsole("MySqlConnectorWrapper.RunCommand",
                    $"Query \"{query.QueryString}\" threw:\n{ex.Message}");
            }

            return queryOutput;
        }


        /// <summary>
        ///     Removes a specific item from the cache, based on the query input.
        /// </summary>
        /// <param name="query">The query related to the item in cache to be removed.</param>
        /// <returns>If it successfully removed the item from the cache.</returns>
        protected bool RemoveItemFromCache(Query query)
        {
            return Configuration.UseCache && _cacheManager.RemoveItemFromCache(query);
        }

        /// <summary>
        ///     Updates the cache's refresh timer with a new time.
        /// </summary>
        /// <param name="rate">The new rate (in ms) that the cache should be refreshed at.</param>
        protected void UpdateCacheRefreshTime(double rate)
        {
            if (!Configuration.UseCache) return;

            _cacheManager.UpdateCacheRefreshTime(rate);
        }

        public void Dispose()
        {
            _cacheManager.Dispose();
            _connectorQueue.Dispose();
        }
    }
}