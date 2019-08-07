using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Pustalorc.Libraries.MySqlConnector.Configuration;
using Pustalorc.Libraries.MySqlConnector.Queries;

namespace Pustalorc.Libraries.MySqlConnector.Queueing
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
        private readonly Connector<T> _connector;

        /// <summary>
        ///     The actual queue for queries.
        /// </summary>
        private readonly Queue<QueueableQuery> _queue = new Queue<QueueableQuery>();

        /// <summary>
        ///     The object to be locked before accessing the queue.
        /// </summary>
        private readonly object _queueLock = new object();

        /// <summary>
        ///     The BackgroundWorker to process the queue.
        /// </summary>
        private readonly BackgroundWorker _queueProcessor = new BackgroundWorker();

        /// <summary>
        ///     Instantiates the connector queue. Requires the instance of the connector.
        /// </summary>
        /// <param name="connector">The instance of the connector being used.</param>
        public ConnectorQueue(Connector<T> connector)
        {
            _connector = connector;

            _queueProcessor.WorkerSupportsCancellation = false;
            _queueProcessor.DoWork += Queue_DoWork;
            _queueProcessor.RunWorkerAsync();
        }

        /// <summary>
        ///     Queues a new QueueableQuery.
        /// </summary>
        /// <param name="item">The query to be queued for execution.</param>
        public void Enqueue(QueueableQuery item)
        {
            lock (_queueLock)
            {
                _queue.Enqueue(item);
            }
        }

        private void Queue_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                QueueableQuery item;

                Thread.Sleep(125);

                lock (_queueLock)
                {
                    if (_queue.Count <= 0)
                        continue;

                    item = _queue.Dequeue();
                }

                switch (item.Query.QueryType)
                {
#pragma warning disable 618
                    // Disabled 618 as this is executed safely on a separate thread which background worker runs from.
                    case EQueryType.Reader:
                        item.QueryCallback(item.Query, _connector.ExecuteReader(item.Query.QueryString));
                        break;
                    case EQueryType.Scalar:
                        item.QueryCallback(item.Query, _connector.ExecuteScalar(item.Query.QueryString));
                        break;
                    case EQueryType.NonQuery:
                        _connector.ExecuteNonQuery(item.Query.QueryString);
                        break;
#pragma warning restore 618
                }
            }
        }
    }
}