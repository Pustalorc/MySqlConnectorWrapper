using System.Collections.Generic;
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
        private readonly ConnectorWrapper<T> _connectorWrapper;

        /// <summary>
        ///     The actual queue for queries.
        /// </summary>
        private readonly Queue<QueueableQuery> _queue = new Queue<QueueableQuery>();

        /// <summary>
        ///     The object to be locked before accessing the queue.
        /// </summary>
        private readonly object _queueLock = new object();

        /// <summary>
        ///     The timer to tick every 125ms to process the queue.
        /// </summary>
        private readonly Timer _tick = new Timer(125);

        /// <summary>
        ///     Instantiates the connector queue. Requires the instance of the connector.
        /// </summary>
        /// <param name="connectorWrapper">The instance of the connector being used.</param>
        internal ConnectorQueue(ConnectorWrapper<T> connectorWrapper)
        {
            _connectorWrapper = connectorWrapper;

            _tick.Elapsed += ProcessQueue;
            _tick.Start();
        }

        /// <summary>
        ///     Enqueues a new QueueableQuery.
        /// </summary>
        /// <param name="item">The query to be enqueued for execution.</param>
        public void Enqueue(QueueableQuery item)
        {
            lock (_queueLock)
            {
                _queue.Enqueue(item);
            }
        }

        private void ProcessQueue(object sender, ElapsedEventArgs e)
        {
            QueueableQuery item;

            lock (_queueLock)
            {
                if (_queue.Count <= 0)
                    return;

                item = _queue.Dequeue();
            }

            item.QueryCallback(item.Query, _connectorWrapper.ExecuteQuery(item.Query));
        }
    }
}