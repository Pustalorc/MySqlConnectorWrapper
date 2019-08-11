using System.Collections.Concurrent;
using System.Timers;
using Pustalorc.Libraries.MySqlConnectorWrapper.Configuration;
using Pustalorc.Libraries.MySqlConnectorWrapper.Queries;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Queueing
{
    /// <summary>
    ///     The queue for the connector. Automatically instantiated when the connector is instantiated.
    /// </summary>
    /// <typeparam name="T">The configuration type passed on to the connector.</typeparam>
    public sealed class ConnectorQueue<T> where T : IConnectorConfiguration
    {
        /// <summary>
        ///     The instance of the connector.
        /// </summary>
        private readonly ConnectorWrapper<T> _connector;

        /// <summary>
        ///     The actual queue for queries.
        /// </summary>
        private readonly ConcurrentQueue<Query> _queue = new ConcurrentQueue<Query>();

        /// <summary>
        ///     The timer to tick every 125ms to process the queue.
        /// </summary>
        private readonly Timer _tick = new Timer(125);

        /// <summary>
        ///     Instantiates the connector queue. Requires the instance of the connector.
        /// </summary>
        /// <param name="connector">The instance of the connector being used.</param>
        internal ConnectorQueue(ConnectorWrapper<T> connector)
        {
            _connector = connector;

            _tick.Elapsed += ProcessQueue;
            _tick.Start();
        }

        /// <summary>
        ///     Enqueues a new QueueableQuery.
        /// </summary>
        /// <param name="item">The query to be enqueued for execution.</param>
        public void Enqueue(Query item)
        {
            _queue.Enqueue(item);
        }

        private void ProcessQueue(object sender, ElapsedEventArgs e)
        {
            if (_queue.Count <= 0 || !_queue.TryDequeue(out var item)) return;

            _connector.ExecuteQuery(item);
        }
    }
}