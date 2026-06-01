using Hdf5DotnetTools.DataTypes;
using Hdf5DotNetTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hdf5DotnetTools
{
#if HDF5_VER1_10
    using hid_t = System.Int64;
#else
    using hid_t = System.Int32;
#endif
    // Example conceptually adapting your code:
    public class SignalDataManager<T> : IDisposable where T : struct
    {
        private readonly Dictionary<string, ChunkedDataset<T>> _signalDatasets = new Dictionary<string, ChunkedDataset<T>>();
        private hid_t _groupId;
        private hid_t _dataGroupId; 

        public SignalDataManager(hid_t groupId)
        {
            _groupId = groupId;
            _dataGroupId = Hdf5.CreateGroup(groupId, "Data"); 
        }

        public void AppendSignals(IList<T[]> signals, string[] signalNames)
        {
            for (int i = 0; i < signals.Count; i++)
            {
                var data = signals[i];
                var name = signalNames[i];

                if (!_signalDatasets.ContainsKey(name))
                {
                    // Initialize a 1D chunked dataset for this specific signal
                    ulong[] chunkSize = new ulong[] { (ulong)data.Length }; // example chunk size
                    var dset = new ChunkedDataset<T>(name, _dataGroupId, chunkSize);  // ← _dataGroupId
                    dset.FirstDataset(data); // You'll need to overload FirstDataset to take short[]
                    _signalDatasets[name] = dset;
                }
                else
                {
                    // Append 1D array
                    _signalDatasets[name].AppendDataset(data);
                }
            }
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
                foreach (var dset in _signalDatasets.Values)
                {
                    dset.Dispose();
                }
                _signalDatasets.Clear();
                Hdf5.CloseGroup(_dataGroupId);  
            }
        }
    }
}
