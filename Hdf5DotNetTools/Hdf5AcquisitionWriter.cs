using HDF.PInvoke;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hdf5DotnetTools.DataTypes;

namespace Hdf5DotNetTools
{


    public class Hdf5AcquisitionFileWriter : IDisposable
    {
        long fileId;
        readonly string _groupName, _filename;
        ChunkedDataset<short> dset = null;
        ulong _nrOfRecords, _sampleCount;
        long _groupId;

        // private readonly ReaderWriterLockSlim lock_ = new ReaderWriterLockSlim();

        public Hdf5AcquisitionFileWriter(string filename, string groupName = "ROOT")
        {
            H5E.set_auto(H5E.DEFAULT, null, IntPtr.Zero);
            //lock_.EnterWriteLock();
            _filename = filename;
            fileId = Hdf5.CreateFile(filename);
            _groupName = groupName;
            _groupId = Hdf5.CreateGroup(fileId, _groupName);

            Header = new Hdf5AcquisitionFile();
            _nrOfRecords = 0;
            _sampleCount = 0;
            //lock_.ExitWriteLock();

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            FileClosed?.Invoke(this, new FileClosedArgs(_filename));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SaveHeader();
                dset?.Dispose();
                var info = Hdf5.GroupInfo(_groupId);
                _groupId = Hdf5.CloseGroup(_groupId);
                fileId = Hdf5.CloseFile(fileId);
            }
        }

        public void SaveHeader()
        {
            //lock_.EnterWriteLock();
            Trace.WriteLine($"saving file {Header.Patient.Name} samples: {_sampleCount}; fileId: {fileId}");
            Header.Recording.EndTime = Header.Recording.StartTime + TimeSpan.FromSeconds(_sampleCount / Header.Recording.SampleRate);
            Header.Recording.NrOfSamples = _sampleCount;
            Header.EventListToEvents();
            for (int i = 0; i < Header.Channels.Count(); i++)
            {
                Header.Channels[i].NrOfSamples = _sampleCount;
            }
            Trace.WriteLine($"writing file {Header.Patient.Name} groupId: {_groupId}; fileId: {fileId}");
            Hdf5.WriteObject(_groupId, Header);
            //lock_.ExitWriteLock();
        }

        /// <summary>
        /// Writes data to the hdf5 file.
        /// </summary>
        public void Write(IEnumerable<double[]> signals)
        {
            //lock_.EnterWriteLock();
            int cols = signals.Count();
            if (cols == 0) return;
            int rows = signals.First().Length;
            if (rows == 0) return;
            //double sr = _header.Recording.SampleRate;

            var data = new short[rows, cols];
            //var byteLength = rows * sizeof(short);
            int i = 0;
            foreach (var sig in signals)
            {
                for (int j = 0; j < rows; j++)
                    data[j, i] = Convert2Short(sig[j], i);
                i++;
            }
            Write(data);
            //lock_.ExitWriteLock();
        }

        /// <summary>
        /// Writes data asynchronously to the hdf5 file.
        /// </summary>
        public async void WriteAsync(IEnumerable<double[]> signals)
        {
            Task writeTask = new Task(() => Write(signals));
            writeTask.Start();
            await writeTask;
        }

        /// <summary>
        /// Writes data to the hdf5 file.
        /// </summary>
        public void Write(short[,] data)
        {
            //lock_.EnterWriteLock();
            if (_nrOfRecords == 0)
            {
                Header.Recording.StartTime = DateTime.Now;
                var dataName = "Data";
                dset = new ChunkedDataset<short>(dataName, _groupId, data);
            }
            else
                dset.AppendDataset(data);
            _sampleCount += (ulong)data.GetLongLength(0);
            _nrOfRecords++;
            //lock_.ExitWriteLock();
        }

        /// <summary>
        /// Writes data asynchronously to the hdf5 file.
        /// </summary>
        public async void WriteAsync(short[,] data)
        {
            Task writeTask = new Task(() => Write(data));
            writeTask.Start();
            await writeTask;
        }

        public short Convert2Short(double val, int channelNr)
        {
            val = (val - Header.Channels[channelNr].Offset) / Header.Channels[channelNr].Amplification;
            //val = val * short.MaxValue;
            if (val > short.MaxValue)
                val = short.MaxValue;
            if (val < short.MinValue)
                val = short.MinValue;
            return Convert.ToInt16(Math.Round(val, MidpointRounding.AwayFromZero));

        }

        public Hdf5AcquisitionFile Header { get; }

        public event EventHandler<FileClosedArgs> FileClosed;

    }


}
