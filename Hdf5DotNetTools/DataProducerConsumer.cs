﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

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
                    if (_queue == null) // || _queue.Count==0
                        return;
                    _queue.TryTake(out T data);
                    if (data != null)
                    {
                        Debug.WriteLine($"item {_queue.Count + 1} in queue will be processed");
                        _action(data);
                    }

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
