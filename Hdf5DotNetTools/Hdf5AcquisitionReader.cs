using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hdf5DotNetTools
{

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
            int cols = _labels.Count();
            var dataName = string.Concat("/", _groupName, "/Data");
            short[,] data = Hdf5.ReadDataset<short>(fileId, dataName, startIndex, endIndex);
            IEnumerable<short> dataEnum = data.Cast<short>();


            int byteLength = sizeof(short) * rows;
            TimeSpan zeroSpan = new TimeSpan(0);
            for (int i = 0; i < _readChannelCnt; i++)
            {
                //_signals.Add(new short[rows]);
                string lbl = _labels[i];
                int nr = _usedChannels[lbl];
                int pos = nr * byteLength;
                var ar = dataEnum.Where((d, j) => (j - nr) % cols == 0);
                _signals.Add(ar.ToArray());
                //Buffer.BlockCopy(data, pos, _signals[i], 0, byteLength);
            }
            return _signals;
        }

        /// <summary>
        /// Reads this instance from a specified start time to a specified end time.
        /// </summary>
        public IList<double[]> ReadDouble()
        {
            return ReadDouble(_header.Recording.StartTime, _header.Recording.EndTime);
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
            ulong endIndex = Convert.ToUInt64(Math.Round(endSpan.TotalSeconds * sr));
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
            ulong endIndex = Convert.ToUInt64(Math.Round(endSpan.TotalSeconds * sr));
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
                fileId = Hdf5.CloseFile(fileId);
        }

        public Hdf5AcquisitionFile Header => _header;

    }

}
