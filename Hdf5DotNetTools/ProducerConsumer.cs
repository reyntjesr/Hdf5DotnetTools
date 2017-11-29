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
    public class ProducerConsumer
    {
        private BlockingCollection<IEnumerable<double[]>>  _queue = new BlockingCollection<IEnumerable<double[]>>();
        private Hdf5AcquisitionFileWriter _writer;
        private int _milliSeconds;

        public ProducerConsumer(Hdf5AcquisitionFileWriter writer, int milliSeconds=0)
        {
            _milliSeconds = milliSeconds;
            _writer = writer;
            var thread = new Thread(StartConsuming)
            {
                IsBackground = true
            };
            thread.Start();
        }

        public void Done()
        {
            _queue.CompleteAdding();
        }

        public void Produce(IEnumerable<double[]> item)
        {
            _queue.Add(item);
        }

        private void StartConsuming()
        {
            while (!_queue.IsCompleted)
            {
                try
                {
                    _queue.TryTake(out IEnumerable<double[]> data);
                    if (data != null)
                        _writer.Write(data);

                }
                catch (InvalidOperationException )
                {
                    Debug.WriteLine(string.Format("Work queue on thread {0} has been closed.", Thread.CurrentThread.ManagedThreadId));
                }
            }
        }
    }
}
