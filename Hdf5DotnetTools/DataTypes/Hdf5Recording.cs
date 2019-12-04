using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Hdf5DotNetTools;

namespace Hdf5DotnetTools.DataTypes
{
    [Hdf5GroupName("Recording")]
    public class Hdf5Recording
    {
        int _nrOfChannels;

        [Hdf5Save(Hdf5Save.DoNotSave)]
        public event PropertyChangedEventHandler PropertyChanged;

        public string Id { get; set; } = "";
        public bool ActiveFilter { get; set; } = false;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; } = DateTime.Now;
        public ulong NrOfSamples { get; set; } = 0;
        public double SampleRate { get; set; } = double.NaN;
        public string Physician { get; set; } = "";
        public string Laborant { get; set; } = "";

        public int NrOfChannels
        {
            get => _nrOfChannels;
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
}
