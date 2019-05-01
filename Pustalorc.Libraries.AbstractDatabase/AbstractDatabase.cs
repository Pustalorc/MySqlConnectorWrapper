using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;

namespace Pustalorc.Libraries.AbstractDatabase
{
    public abstract class AbstractDatabase<T> where T : AbstractDatabaseConfiguration
    {
        // Cache needs to be changed to something more optimal, specially if many queries will/can be executed.
        private readonly List<Cache> _cache = new List<Cache>();
        private readonly DatabaseQueue _databaseQueue;
        protected readonly T Configuration;

        protected AbstractDatabase(T configuration)
        {
            Configuration = configuration;
            _databaseQueue = new DatabaseQueue();
            _databaseQueue.OnQueueProcess += ProcessQueueableQuery;

            var connection = CreateConnection();

            try
            {
                connection.Open();
                connection.Close();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042)
                    ConsoleWrite("AbstractDatabase.Constructor", "Can't connect to MySQL host.");
                else
                    ConsoleWrite("AbstractDatabase.Constructor", ex);
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }
        }

        private void ProcessQueueableQuery(QueueableQuery query)
        {
#pragma warning disable 618
            // Call from the queue.
            switch (query.QueryType)
            {
                case EQueueableType.Reader:
                    if (!Configuration.UseSeparateThread) ExecuteReader(query.Query);

                    ThreadPool.QueueUserWorkItem(o => ExecuteReader(query.Query));
                    break;
                case EQueueableType.Scalar:
                    if (!Configuration.UseSeparateThread) ExecuteScalar(query.Query);

                    ThreadPool.QueueUserWorkItem(o => ExecuteScalar(query.Query));
                    break;
                case EQueueableType.NonQuery:
                    if (!Configuration.UseSeparateThread) ExecuteNonQuery(query.Query);

                    ThreadPool.QueueUserWorkItem(o => ExecuteNonQuery(query.Query));
                    break;
            }
#pragma warning restore 618
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void ConsoleWrite(object source, object message, ConsoleColor consoleColor = ConsoleColor.Green)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"[{source}]: {message}");
            Console.ResetColor();
        }

        protected virtual void Write(object source, object message, ConsoleColor consoleColor = ConsoleColor.Green)
        {
            ConsoleWrite(source, message, consoleColor);
        }

        protected MySqlConnection CreateConnection()
        {
            MySqlConnection connection = null;

            try
            {
                if (Configuration.DatabasePort == 0)
                    Configuration.DatabasePort = 3306;
                connection = new MySqlConnection(
                    $"SERVER={Configuration.DatabaseAddress};DATABASE={Configuration.DatabaseName};UID={Configuration.DatabaseUsername};PASSWORD={Configuration.DatabasePassword};PORT={Configuration.DatabasePort};");
            }
            catch (Exception ex)
            {
                Write("AbstractDatabase.CreateConnection", ex);
            }

            return connection;
        }

        protected void RequestNonQuery(string query)
        {
            _databaseQueue.Enqueue(new QueueableQuery(query, EQueueableType.NonQuery));
        }

        protected void RequestMultipleNonQuery(IEnumerable<string> queries)
        {
            foreach (var query in queries)
                RequestNonQuery(query);
        }

        protected IEnumerable<Row> RequestReader(string query)
        {
#pragma warning disable 618
            // Reader MUST be directly executed here.
            if (!Configuration.UseCache) return ExecuteReader(query);

            var cache = _cache.Find(k =>
                !string.IsNullOrEmpty(k?.Query) &&
                string.Equals(k.Query, query, StringComparison.InvariantCultureIgnoreCase));
            if (cache == null) return ExecuteReader(query);
#pragma warning restore 618

            _databaseQueue.Enqueue(new QueueableQuery(query, EQueueableType.Reader));
            return (List<Row>) cache.Output;
        }

        protected object RequestScalar(string query)
        {
#pragma warning disable 618
            // Scalar MUST be directly executed here.
            if (!Configuration.UseCache) return ExecuteScalar(query);

            var cache = _cache.Find(k =>
                !string.IsNullOrEmpty(k?.Query) &&
                string.Equals(k.Query, query, StringComparison.InvariantCultureIgnoreCase));
            if (cache == null) return ExecuteScalar(query);
#pragma warning restore 618

            _databaseQueue.Enqueue(new QueueableQuery(query, EQueueableType.Scalar));
            return cache.Output;
        }

        [Obsolete("Do not call this method directly unless it's on startup or it's part of the queue execution.")]
        protected void ExecuteNonQuery(string query)
        {
            var connection = CreateConnection();

            try
            {
                var command = connection.CreateCommand();
                command.CommandText = query;

                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception ex)
            {
                Write("AbstractDatabase.ExecuteNonQuery", $"Query \"{query}\" threw:\n{ex}");
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }
        }

        [Obsolete("Do not call this method directly unless it's on startup or it's part of the queue execution.")]
        protected IEnumerable<Row> ExecuteReader(string query)
        {
            var result = new List<Row>();
            var connection = CreateConnection();
            MySqlDataReader reader = null;

            try
            {
                var command = connection.CreateCommand();

                command.CommandText = query;

                connection.Open();

                reader = command.ExecuteReader();
                while (reader.Read())
                    try
                    {
                        var values = new Dictionary<string, object>();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);
                            values.Add(columnName, reader[columnName]);
                        }

                        result.Add(new Row {Values = values});
                    }
                    catch (Exception ex)
                    {
                        Write("AbstractDatabase.Reader", $"Query \"{query}\" threw:\n{ex}");
                    }

                reader.Close();

                connection.Close();
            }
            catch (Exception ex)
            {
                Write("AbstractDatabase.ExecuteReader", $"Query \"{query}\" threw:\n{ex}");
            }
            finally
            {
                if (reader?.IsClosed == false)
                    reader.Close();

                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            if (!Configuration.UseCache) return result;

            var cache = _cache.Find(k =>
                !string.IsNullOrEmpty(k?.Query) &&
                string.Equals(k.Query, query, StringComparison.InvariantCultureIgnoreCase));
            if (cache == null)
            {
                _cache.Add(new Cache(query, result));
                return result;
            }

            var updated = new Cache(cache.Query, result);
            _cache.Remove(cache);
            _cache.Add(updated);
            return result;
        }

        [Obsolete("Do not call this method directly unless it's on startup or it's part of the queue execution.")]
        protected object ExecuteScalar(string query)
        {
            object result = null;
            var connection = CreateConnection();

            try
            {
                var command = connection.CreateCommand();

                command.CommandText = query;

                connection.Open();
                result = command.ExecuteScalar();
                connection.Close();
            }
            catch (Exception ex)
            {
                Write("AbstractDatabase.ExecuteScalar", $"Query \"{query}\" threw:\n{ex}");
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }

            if (!Configuration.UseCache) return result;

            var cache = _cache.Find(k =>
                !string.IsNullOrEmpty(k?.Query) &&
                string.Equals(k.Query, query, StringComparison.InvariantCultureIgnoreCase));
            if (cache == null)
            {
                _cache.Add(new Cache(query, result));
                return result;
            }

            var updated = new Cache(cache.Query, result);
            _cache.Remove(cache);
            _cache.Add(updated);
            return result;
        }
    }
}