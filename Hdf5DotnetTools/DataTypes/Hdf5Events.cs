using System;
using System.Collections.Generic;
using System.Text;
using Hdf5DotNetTools;

namespace Hdf5DotnetTools.DataTypes
{
    [Hdf5GroupName("Events")]
    public struct Hdf5Events
    {
        public string[] Events { get; set; }
        public DateTime[] Times { get; set; }
        public TimeSpan[] Durations { get; set; }
        public Hdf5Events(int length)
        {
            Events = new string[length];
            Times = new DateTime[length];
            Durations = new TimeSpan[length];
        }

    }
}
