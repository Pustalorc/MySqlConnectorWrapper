using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;

namespace Pustalorc.Libraries.AbstractDatabase
{
    public sealed class DatabaseQueue
    {
        public delegate void QueueProcessed(QueueableQuery query);

        private readonly Queue<QueueableQuery> _queue = new Queue<QueueableQuery>();
        private readonly object _queueLock = new object();
        private readonly BackgroundWorker _queueProcessor = new BackgroundWorker();
        private readonly Timer _queueProcessorChecker = new Timer(10000);

        public DatabaseQueue()
        {
            _queueProcessor.WorkerSupportsCancellation = false;
            _queueProcessor.DoWork += Queue_DoWork;

            _queueProcessorChecker.Elapsed += CheckQueueProcessor;
            _queueProcessorChecker.Start();
        }

        public event QueueProcessed OnQueueProcess;

        private void CheckQueueProcessor(object sender, ElapsedEventArgs e)
        {
            if (_queue.Count > 0 && !_queueProcessor.IsBusy)
                _queueProcessor.RunWorkerAsync();
        }

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

                var skipEmptyCheck = false;

                lock (_queueLock)
                {
                    if (_queue.Count <= 0)
                        return;

                    if (_queue.Count > 1)
                        skipEmptyCheck = true;

                    item = _queue.Dequeue();
                }

                OnQueueProcess?.Invoke(item);

                if (!skipEmptyCheck) continue;

                lock (_queueLock)
                {
                    if (_queue.Count <= 0)
                        return;
                }
            }
        }
    }
}