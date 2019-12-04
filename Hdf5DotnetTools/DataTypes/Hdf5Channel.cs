using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Hdf5DotNetTools;

namespace Hdf5DotnetTools.DataTypes
{
    [Hdf5GroupName("Channel")]
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
        public ulong NrOfSamples;

    }
}
