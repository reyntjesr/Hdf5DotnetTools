using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hdf5DotNetTools
{
    public class DataProducerConsumer<T> : IDisposable
    {
        private BlockingCollection<T> _queue = new BlockingCollection<T>();
        private readonly Action<T> _action;
        private readonly int _milliSeconds;

        public DataProducerConsumer(Action<T> action, int milliSeconds = 0)
        {
            _milliSeconds = milliSeconds;
            _action = action;

            var thread = new Thread(StartConsuming)
            {
                IsBackground = true
            };
            thread.Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _queue.Dispose();
            }
        }

        public void Done()
        {
            _queue.CompleteAdding();
        }

        public void Produce(T item)
        {
            _queue.Add(item);
        }

        private void StartConsuming()
        {
            while (!_queue.IsCompleted)
            {
                try
                {
                    if (_queue == null)
                        return;
                    _queue.TryTake(out T data);
                    if (data != null)
                        _action(data);

                }
                catch (InvalidOperationException)
                {
                    Debug.WriteLine(string.Format("Work queue on thread {0} has been closed.", Thread.CurrentThread.ManagedThreadId));
                }
            }
            IsDone?.Invoke(this, new EventArgs());
            Dispose();
        }

        public event EventHandler IsDone;
    }
}
