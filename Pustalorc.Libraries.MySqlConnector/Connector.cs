using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using Pustalorc.Libraries.MySqlConnector.Caching;
using Pustalorc.Libraries.MySqlConnector.Configuration;
using Pustalorc.Libraries.MySqlConnector.Delegates;
using Pustalorc.Libraries.MySqlConnector.Queueing;
using Pustalorc.Libraries.MySqlConnector.TableStructure;

namespace Pustalorc.Libraries.MySqlConnector
{
    /// <summary>
    ///     The connector. Instantiate it and pass a configuration class to it.
    /// </summary>
    /// <typeparam name="T">The type, which inherits from IConnectorConfiguration, which should be used by the connector.</typeparam>
    public sealed class Connector<T> where T : IConnectorConfiguration
    {
        /// <summary>
        ///     The queue that the connector should use.
        /// </summary>
        private readonly ConnectorQueue<T> _connectorQueue;

        /// <summary>
        ///     The caching system that the connector should use.
        /// </summary>
        private readonly SmartCache<T> _smartCache;

        /// <summary>
        ///     The original unmodified passed configuration to the class.
        /// </summary>
        public readonly T Configuration;

        /// <summary>
        ///     The connection to the MySql database.
        /// </summary>
        private MySqlConnection _connection;

        /// <summary>
        ///     Default constructor, only requires an instance of type T to be used as main configuration.
        /// </summary>
        /// <param name="configuration">The instance of type T to be used as main configuration</param>
        public Connector(T configuration)
        {
            Configuration = configuration;

            _smartCache = new SmartCache<T>(this);
            _connectorQueue = new ConnectorQueue<T>(this);

            try
            {
                Connection.Open();
                Connection.Close();
            }
            catch (MySqlException ex)
            {
                LogConsole("DatabaseConnector.Constructor",
                    ex.Number == 1042 ? "Can't connect to MySQL host." : ex.Message);
            }
            finally
            {
                if (Connection.State != ConnectionState.Closed)
                    Connection.Close();
            }
        }

        /// <summary>
        ///     Property to store a single connection and create it if it's broken or doesn't exist.
        /// </summary>
        private MySqlConnection Connection
        {
            get
            {
                if (_connection == null || _connection.State == ConnectionState.Broken)
                    _connection = CreateConnection();
                return _connection;
            }
        }

        /// <summary>
        ///     Log a message to console.
        /// </summary>
        /// <param name="source">Specific source of the message.</param>
        /// <param name="message">The message to be logged to console.</param>
        /// <param name="consoleColor">The color to be used for the message in console.</param>
        private void LogConsole(object source, object message, ConsoleColor consoleColor = ConsoleColor.Green)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"[{source}]: {message}");
            Console.ResetColor();
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
                    $"SERVER={Configuration.DatabaseAddress};DATABASE={Configuration.DatabaseName};UID={ToValidValue(Configuration.DatabaseUsername)};PASSWORD={ToValidValue(Configuration.DatabasePassword)};PORT={Configuration.DatabasePort};");
            }
            catch (Exception ex)
            {
                LogConsole("AbstractDatabase.CreateConnection", ex.Message);
            }

            return connection;
        }

        /// <summary>
        ///     Transforms the input into a valid encapsulated value that can be used in a MySql ConnectionString.
        /// </summary>
        /// <param name="input">The value to encapsulate.</param>
        /// <returns>The input but encapsulated.</returns>
        private string ToValidValue(string input)
        {
            if (!input.Contains(";") && !input.Contains("'") && !input.Contains("\"")) return input;

            if (!input.Contains("'") && !input.Contains("\"")) return $"\"{input}\"";

            if (!input.Contains("'")) return $"'{input}'";

            if (!input.Contains("\"")) return $"\"{input}\"";

            return input.StartsWith("\"", StringComparison.InvariantCulture)
                ? $"'{RepeatChar(input, '\'')}'"
                : $"\"{RepeatChar(input, '\"')}\"";
        }

        /// <summary>
        ///     Repeats a specific character in the string if found.
        /// </summary>
        /// <param name="input">The input string to cycle through.</param>
        /// <param name="character">The character to repeat.</param>
        /// <returns>A new string with the selected character repeated.</returns>
        private string RepeatChar(string input, char character)
        {
            var output = "";

            foreach (var c in input)
            {
                if (c == character)
                    output += c;

                output += c;
            }

            return output;
        }

        /// <summary>
        ///     Requests to execute "NonQuery" queries (Updates, Inserts, Deletes, etc. [ie: no output expected])
        /// </summary>
        /// <param name="queries">The queries to be executed.</param>
        public void RequestNonQuery(params string[] queries)
        {
            foreach (var query in queries)
                _connectorQueue.Enqueue(new QueueableQuery(query, EQueueableQueryType.NonQuery));
        }

        /// <summary>
        ///     Requests to execute "Reader" queries (Select * or similar queries [ie: Requires the return of many rows with many
        ///     columns, or a single row but with many columns])
        /// </summary>
        /// <param name="callback">The callback function/delegate for the result for each query.</param>
        /// <param name="queries">The queries to be executed.</param>
        public void RequestReader(ReaderCallback callback, params string[] queries)
        {
            // Code must be re-written to be compliant with changes to execution: Query shall execute at all times and call the callback once complete.
            // Cache shall also be updated on its own separate timer within the Cache object.
            foreach (var query in queries)
            {
                if (!Configuration.UseCache)
                {
                    callback(query, ExecuteReader(query));
                    return;
                }

                var cache = _smartCache.GetItemInCache(query);
                if (cache == null)
                {
                    callback(query, ExecuteReader(query));
                    return;
                }

                _connectorQueue.Enqueue(new QueueableQuery(query, EQueueableQueryType.Reader));
                callback(query, (List<Row>) cache.Output);
            }
        }

        /// <summary>
        ///     Requests to execute "Scalar" queries (Select `Column` or similar queries [ie: Requires the return of a single
        ///     result, commonly a single column from a single row])
        /// </summary>
        /// <param name="callback">The callback function/delegate for the result for each query.</param>
        /// <param name="queries">The queries to be executed.</param>
        public void RequestScalar(ScalarCallback callback, params string[] queries)
        {
            // Code must be re-written to be compliant with changes to execution: Query shall execute at all times and call the callback once complete.
            // Cache shall also be updated on its own separate timer within the Cache object.
            foreach (var query in queries)
            {
                if (!Configuration.UseCache)
                {
                    callback(query, ExecuteScalar(query));
                    return;
                }

                var cache = _smartCache.GetItemInCache(query);
                if (cache == null)
                {
                    callback(query, ExecuteScalar(query));
                    return;
                }

                _connectorQueue.Enqueue(new QueueableQuery(query, EQueueableQueryType.Scalar));
                callback(query, cache.Output);
            }
        }

        /// <summary>
        ///     Directly executes a "NonQuery" query on the mysql server, bypassing the queue.
        /// </summary>
        /// <param name="query">The query to be executed.</param>
        public void ExecuteNonQuery(string query)
        {
            try
            {
                var command = Connection.CreateCommand();
                command.CommandText = query;

                Connection.Open();
                command.ExecuteNonQuery();
                Connection.Close();
            }
            catch (Exception ex)
            {
                LogConsole("AbstractDatabase.ExecuteNonQuery", $"Query \"{query}\" threw:\n{ex.Message}");
            }
            finally
            {
                if (Connection.State != ConnectionState.Closed)
                    Connection.Close();
            }
        }

        /// <summary>
        ///     Directly executes a "Reader" query on the mysql server, bypassing the queue and cache.
        /// </summary>
        /// <param name="query">The query to be executed.</param>
        public IEnumerable<Row> ExecuteReader(string query)
        {
            var result = new List<Row>();
            MySqlDataReader reader = null;

            try
            {
                var command = Connection.CreateCommand();

                command.CommandText = query;

                Connection.Open();

                reader = command.ExecuteReader();
                while (reader.Read())
                    try
                    {
                        var columns = new List<Column>();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);
                            columns.Add(new Column {Name = columnName, Value = reader[columnName]});
                        }

                        result.Add(new Row {Columns = columns});
                    }
                    catch (Exception ex)
                    {
                        LogConsole("AbstractDatabase.Reader", $"Query \"{query}\" threw:\n{ex.Message}");
                    }

                reader.Close();

                Connection.Close();
            }
            catch (Exception ex)
            {
                LogConsole("AbstractDatabase.ExecuteReader", $"Query \"{query}\" threw:\n{ex.Message}");
            }
            finally
            {
                if (reader?.IsClosed == false)
                    reader.Close();

                if (Connection.State != ConnectionState.Closed)
                    Connection.Close();
            }

            if (Configuration.UseCache) _smartCache.UpdateStoreItemInCache(query, result);

            return result;
        }

        /// <summary>
        ///     Directly executes a "Scalar" query on the mysql server, bypassing the queue and cache.
        /// </summary>
        /// <param name="query">The query to be executed.</param>
        public object ExecuteScalar(string query)
        {
            object result = null;

            try
            {
                var command = Connection.CreateCommand();

                command.CommandText = query;

                Connection.Open();
                result = command.ExecuteScalar();
                Connection.Close();
            }
            catch (Exception ex)
            {
                LogConsole("AbstractDatabase.ExecuteScalar", $"Query \"{query}\" threw:\n{ex.Message}");
            }
            finally
            {
                if (Connection.State != ConnectionState.Closed)
                    Connection.Close();
            }

            if (Configuration.UseCache) _smartCache.UpdateStoreItemInCache(query, result);

            return result;
        }
    }
}