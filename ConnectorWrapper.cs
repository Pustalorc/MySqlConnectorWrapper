using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Pustalorc.Libraries.MySqlConnectorWrapper.Caching;
using Pustalorc.Libraries.MySqlConnectorWrapper.Configuration;
using Pustalorc.Libraries.MySqlConnectorWrapper.Delegates;
using Pustalorc.Libraries.MySqlConnectorWrapper.Queries;
using Pustalorc.Libraries.MySqlConnectorWrapper.Queueing;
using Pustalorc.Libraries.MySqlConnectorWrapper.TableStructure;

namespace Pustalorc.Libraries.MySqlConnectorWrapper
{
    /// <summary>
    ///     The connector. Inherit it and pass a configuration class to it.
    /// </summary>
    /// <typeparam name="T">The type, which inherits from IConnectorConfiguration, which should be used by the connector.</typeparam>
    public abstract class ConnectorWrapper<T> where T : IConnectorConfiguration
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
        ///     The connection to the MySql database.
        /// </summary>
        private MySqlConnection _connection;

        /// <summary>
        ///     Default constructor, only requires an instance of type T to be used as main configuration.
        /// </summary>
        /// <param name="configuration">The instance of type T to be used as main configuration</param>
        protected ConnectorWrapper(T configuration)
        {
            Configuration = configuration;

            if (configuration.UseCache) _cacheManager = new CacheManager<T>(this);

            _connectorQueue = new ConnectorQueue<T>(this);

            try
            {
                Connection.Open();
            }
            catch (MySqlException ex)
            {
                Utils.LogConsole("MySqlConnectorWrapper.Constructor",
                    ex.Number == 1042 ? "Can't connect to MySQL host." : ex.Message);
            }
            finally
            {
                Connection.Close();
            }
        }

        /// <summary>
        ///     Property to store a single connection and create it if it doesn't exist.
        /// </summary>
        private MySqlConnection Connection => _connection ?? (_connection = CreateConnection());

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
        /// <param name="callback">The callback function/delegate for the result for each query.</param>
        /// <param name="queries">The queries to be executed.</param>
        public void RequestQuery(QueryCallback callback, params Query[] queries)
        {
            foreach (var query in queries)
            {
                if (Configuration.UseCache)
                {
                    var cache = _cacheManager.GetItemInCache(query);
                    if (cache != null)
                    {
                        callback(query, cache.Output);
                        continue;
                    }
                }

                _connectorQueue.Enqueue(new QueueableQuery(query, callback));
            }
        }

        /// <summary>
        ///     Executes a query, ignoring any caching, queueing or multi-threading.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>The result of the query executed.</returns>
        public object ExecuteQuery(Query query)
        {
            object result = null;
            MySqlDataReader reader = null;

            try
            {
                var command = Connection.CreateCommand();
                command.CommandText = query.QueryString;

                foreach (var param in query.QueryParameters)
                    command.Parameters.AddWithValue(param.Name, param.Value);

                Connection.Open();
                switch (query.QueryType)
                {
                    case EQueryType.NonQuery:
                        result = command.ExecuteNonQuery();
                        break;
                    case EQueryType.Scalar:
                        result = command.ExecuteScalar();
                        break;
                    case EQueryType.Reader:
                        var readerResult = new List<Row>();

                        reader = command.ExecuteReader();
                        while (reader.Read())
                            try
                            {
                                var columns = new List<Column>();

                                for (var i = 0; i < reader.FieldCount; i++)
                                    columns.Add(new Column(reader.GetName(i), reader.GetValue(i)));

                                readerResult.Add(new Row(columns));
                            }
                            catch (Exception ex)
                            {
                                Utils.LogConsole("MySqlConnectorWrapper.Reader",
                                    $"Query \"{query}\" threw:\n{ex.Message}");
                            }

                        result = readerResult;
                        break;
                }
            }
            catch (Exception ex)
            {
                Utils.LogConsole("MySqlConnectorWrapper.ExecuteQuery", $"Query \"{query}\" threw:\n{ex.Message}");
            }
            finally
            {
                reader?.Close();
                Connection.Close();
            }

            _cacheManager?.StoreItemInCache(query, result);
            return result;
        }

        /// <summary>
        ///     Removes a specific item from the cache, based on the query input.
        /// </summary>
        /// <param name="query">The query related to the item in cache to be removed.</param>
        /// <returns>If it successfully removed the item from the cache.</returns>
        public bool RemoveItemFromCache(Query query)
        {
            return _cacheManager.RemoveItemFromCache(query);
        }
    }
}