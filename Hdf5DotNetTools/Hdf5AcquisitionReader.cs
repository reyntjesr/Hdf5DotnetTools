using System;
using System.Collections.Generic;
using System.Linq;
using HDF.PInvoke;

namespace Hdf5DotNetTools
{
#if HDF5_VER1_10
    using hid_t = System.Int64;
#else
    using hid_t = System.Int32;
#endif

    /*struct ReadInfo {
        public ReadInfo()
        {

        }
    }*/

    public class Hdf5AcquisitionFileReader : IDisposable
    {

        long fileId;
        Hdf5AcquisitionFile _header;
        IList<string> _labels;
        IList<short[]> _signals;
        Dictionary<string, short> _usedChannels;
        readonly int /*_fileChannelCnt,*/ _readChannelCnt;
        readonly string _groupName;
        readonly bool _isDatasetPerSignal;


        /// <summary>
        /// Initializes a new instance of the <see cref="Hdf5AcquisitionFileReader"/> class.
        /// </summary>
        /// <param name="filename">A filename.</param>
        /// <param name="groupName">a root group. If not specified used "ROOT</param>
        public Hdf5AcquisitionFileReader(string filename, string[] labels = null, string groupName = "ROOT")
        {
            fileId = Hdf5.OpenFile(filename, readOnly: true);
            _header = Hdf5.ReadObject<Hdf5AcquisitionFile>(fileId, groupName);
            _groupName = groupName;

            H5E.set_auto(H5E.DEFAULT, null, IntPtr.Zero);
            var dataName = string.Concat("/", _groupName, "/Data");
            long groupGroupId = H5G.open(fileId, dataName);
            if (groupGroupId >= 0)
            {
                _isDatasetPerSignal = true;
                H5G.close(groupGroupId);
            }
            else
            {
                _isDatasetPerSignal = false;
            }

            _usedChannels = new Dictionary<string, short>();
            for (short i = 0; i < _header.Recording.NrOfChannels; i++)
                _usedChannels.Add(_header.Channels[i].Label, i);
            if (labels == null)
                _labels = _header.Channels.Select(c => c.Label).ToList();
            else
                _labels = labels;
            _readChannelCnt = _labels.Count;
            _signals = new List<short[]>(_readChannelCnt);
        }

        /// <summary>
        /// Reads this instance.
        /// </summary>
        public IList<short[]> Read()
        {
            return Read(_header.Recording.StartTime, _header.Recording.EndTime);
        }

        /// <summary>
        /// Reads this instance from a specified start index to a specified end index.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        public IList<short[]> Read(ulong startIndex, ulong endIndex)
        {
            _signals.Clear();
            int rows = Convert.ToInt32(endIndex - startIndex + 1);
            var dataName = string.Concat("/", _groupName, "/Data");

            if (!_isDatasetPerSignal)
            {
                int cols = _labels.Count();
                short[,] data = Hdf5.ReadDataset<short>(fileId, dataName, startIndex, endIndex);
                IEnumerable<short> dataEnum = data.Cast<short>();

                int byteLength = sizeof(short) * rows;
                for (int i = 0; i < _readChannelCnt; i++)
                {
                    string lbl = _labels[i];
                    int nr = _usedChannels[lbl];
                    var ar = dataEnum.Where((d, j) => (j - nr) % cols == 0);
                    _signals.Add(ar.ToArray());
                }
            }
            else
            {
                for (int i = 0; i < _readChannelCnt; i++)
                {
                    string lbl = _labels[i];
                    var channelDataName = string.Concat(dataName, "/", lbl);
                    short[] data = Hdf5.ReadDataset1D<short>(fileId, channelDataName, startIndex, endIndex);
                    _signals.Add(data);
                }
            }
            return _signals;
        }

        /// <summary>
        /// Reads this instance from a specified start time to a specified end time.
        /// </summary>
        public IList<double[]> ReadDouble()
        {
            TimeSpan oneSample = TimeSpan.FromSeconds(1 / _header.Recording.SampleRate);
            return ReadDouble(_header.Recording.StartTime, _header.Recording.EndTime.Subtract(oneSample));
        }

        /// <summary>
        /// Reads this instance from a specified start time to a specified end time.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        public IList<double[]> ReadDouble(ulong startIndex, ulong endIndex)
        {
            Read(startIndex, endIndex);
            int rows = Convert.ToInt32(endIndex - startIndex + 1);
            var dblList = new List<double[]>(_readChannelCnt);
            for (int i = 0; i < _readChannelCnt; i++)
            {
                var sig = _signals[i];
                string lbl = _labels[i];
                int nr = _usedChannels[lbl];
                double amp = _header.Channels[nr].Amplification;
                double offset = _header.Channels[nr].Offset;
                dblList.Add(sig.Select(s => s * amp + offset).ToArray());

            }
            return dblList;
        }

        /// <summary>
        /// Reads this instance from a specified start time to a specified end time.
        /// </summary>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        public IList<double[]> ReadDouble(DateTime startTime, DateTime endTime)
        {
            double sr = _header.Recording.SampleRate;
            TimeSpan startSpan = startTime - _header.Recording.StartTime;
            TimeSpan endSpan = endTime - _header.Recording.StartTime;
            ulong startIndex = Convert.ToUInt64(Math.Round(startSpan.TotalSeconds * sr, MidpointRounding.AwayFromZero));
            ulong endIndex = Convert.ToUInt64(Math.Round(endSpan.TotalSeconds * sr)) - 1;
            return ReadDouble(startIndex, endIndex);
        }

        /// <summary>
        /// Reads this instance from a specified start time to a specified end time.
        /// </summary>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        public IList<short[]> Read(DateTime startTime, DateTime endTime)
        {
            double sr = _header.Recording.SampleRate;
            TimeSpan startSpan = startTime - _header.Recording.StartTime;
            TimeSpan endSpan = endTime - _header.Recording.StartTime;
            ulong startIndex = Convert.ToUInt64(Math.Round(startSpan.TotalSeconds * sr, MidpointRounding.AwayFromZero));
            ulong endIndex = Convert.ToUInt64(Math.Round(endSpan.TotalSeconds * sr)) - 1;
            return Read(startIndex, endIndex);
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
                fileId = Hdf5.CloseFile(fileId);
                _signals.Clear();
            }
        }

        public Hdf5AcquisitionFile Header => _header;

    }

}
