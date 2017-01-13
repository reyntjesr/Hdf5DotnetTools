using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hdf5DotNetTools
{

    public class Hdf5AcquisitionFileReader
    {

        long fileId;
        Hdf5AcquisitionFile _header;
        IList<string> _labels;
        IList<short[]> _signals;
        Dictionary<string, short> _usedChannels;
        int /*_fileChannelCnt,*/ _readChannelCnt;

        readonly string groupName = "EEG";


        /// <summary>
        /// Initializes a new instance of the <see cref="Hdf5AcquisitionFileReader"/> class.
        /// </summary>
        /// <param name="aFilename">A filename.</param>
        /// <param name="aMode">A mode enumeration that specifies how the file system should open a file.</param>
        public Hdf5AcquisitionFileReader(string aFilename, bool checking = false, string[] labels = null)
        {
            fileId = Hdf5.OpenFile(aFilename, readOnly: true);
            _header = Hdf5.ReadObject(fileId, _header, groupName);

            _usedChannels = new Dictionary<string, short>();
            for (short i = 0; i < _header.Recording.NrOfChannels; i++)
                _usedChannels.Add(_header.Channels.Labels[i], i);
            _labels = labels ?? _header.Channels.Labels;
            _readChannelCnt = _labels.Count();
            _signals = new List<short[]>(_readChannelCnt);
        }

        /// <summary>
        /// Reads this instance.
        /// </summary>
        public void Read()
        {
            Read(_header.Recording.StartTime, _header.Recording.EndTime);
        }

        /// <summary>
        /// Reads this instance from a specified start index to a specified end index.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        public IList<short[]> Read(ulong startIndex, ulong endIndex)
        {
            int rows = Convert.ToInt32(endIndex - startIndex);
            int cols = _labels.Count();
            short[,] data = Hdf5.ReadDataset<short>(fileId, "Data", startIndex, endIndex);
           
            int byteLength = sizeof(short) * rows;
            TimeSpan zeroSpan = new TimeSpan(0);
            for (int i = 0; i < _readChannelCnt; i++)
            {
                var sig = _signals[i];
                string lbl = _labels[i];
                int nr = _usedChannels[lbl];
                int pos = nr * byteLength;
                _signals[i] = new short[rows];
                Buffer.BlockCopy(data, pos, _signals[i], 0, byteLength);
            }
            return _signals;
        }

        /// <summary>
        /// Reads this instance from a specified start time to a specified end time.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        public IList<double[]> ReadDouble(ulong startIndex, ulong endIndex)
        {
            Read(startIndex, endIndex);
            int rows = Convert.ToInt32(endIndex - startIndex);
            var dblList = new List<double[]>(_readChannelCnt);
            for (int i = 0; i < _readChannelCnt; i++)
            {
                var sig = _signals[i];
                string lbl = _labels[i];
                int nr = _usedChannels[lbl];
                double amp = _header.Channels.Amplifications[nr];
                double offset = _header.Channels.Offsets[nr];
                dblList[i] =sig.Select(s => s * amp + offset).ToArray();

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
        public void Read(DateTime startTime, DateTime endTime)
        {
            double sr = _header.Recording.SampleRate;
            TimeSpan startSpan = startTime - _header.Recording.StartTime;
            TimeSpan endSpan = endTime - _header.Recording.StartTime;
            ulong startIndex = Convert.ToUInt64(Math.Round(startSpan.TotalSeconds * sr, MidpointRounding.AwayFromZero));
            ulong endIndex = Convert.ToUInt64(Math.Round(endSpan.TotalSeconds * sr));
            Read(startIndex, endIndex);
        }
    }

    public class Hdf5AcquisitionFileWriter
    {

        public Hdf5AcquisitionFileWriter()
        {

        }
    }

    [Hdf5SaveAttribute(Hdf5Save.Save)]
    public class Hdf5AcquisitionFile
    {
        public Hdf5AcquisitionFile()
        {
            Patient = new Hdf5Patient();
            Recording = new Hdf5Recording();
            Events = new Hdf5Events();
            //Events = new Hdf5Event[0];

            Recording.PropertyChanged += (sender, eventArgs) =>
            {
                if (eventArgs.PropertyName == nameof(Hdf5Recording.NrOfChannels))
                    Channels = new Hdf5Channels(Recording.NrOfChannels);
            };

        }

        public Hdf5Patient Patient { get; set; }
        public Hdf5Recording Recording { get; set; }

        //[Hdf5Save(Hdf5Save.DoNotSave)]
        //public Hdf5Channel[] Channels { get; set; }
        public Hdf5Channels Channels { get; set; }

        //[Hdf5Save(Hdf5Save.DoNotSave)]
        public Hdf5Events Events { get; set; }

        [Hdf5Save(Hdf5Save.DoNotSave)]
        public short[,] Data { get; set; }



    }

    [Hdf5GroupName("Channels")]
    public class Hdf5Channels
    {
        public Hdf5Channels(int length)
        {
            Labels = new string[length];
            Dimensions = new string[length];
            Amplifications = new double[length];
            Offsets = new double[length];
            SamplingRates = new double[length];
            NrOfSamples = new int[length];
        }
        public string[] Labels { get; set; }
        public string[] Dimensions { get; set; }
        public double[] Amplifications { get; set; }
        public double[] Offsets { get; set; }
        public double[] SamplingRates { get; set; }
        public int[] NrOfSamples { get; set; }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Hdf5Channel
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string Label;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)]
        public string Dimension;
        public double Amplification;
        public double Offset;
        public double SamplingRate;
        public int NrOfSamples;

    }

    [Hdf5GroupName("Recording")]
    public class Hdf5Recording
    {
        int _nrOfChannels;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Id { get; set; }
        public bool ActiveFilter { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public ulong NrOfSamples { get; set; }
        public double SampleRate { get; set; } = double.NaN;
        public string Physician { get; set; }
        public string Laborant { get; set; }

        public int NrOfChannels
        {
            get { return _nrOfChannels; }
            set
            {
                if (_nrOfChannels != value)
                {
                    _nrOfChannels = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NrOfChannels)));
                }
            }
        }
    }

    [Hdf5GroupName("Patient")]
    public class Hdf5Patient
    {
        public string Name;
        public string Id;
        public int RecId;
        public string Gender;
        public DateTime BirthDate;
        public double Height;
        public double Weight;
        public DateTime EditData;
    }

    [Hdf5GroupName("Events")]
    public struct Hdf5Events
    {
        public Hdf5Events(int length)
        {
            Events = new string[length];
            Times = new DateTime[length];
            Durations = new TimeSpan[length];
        }
        public string[] Events;
        public DateTime[] Times;
        public TimeSpan[] Durations;
    }

}
