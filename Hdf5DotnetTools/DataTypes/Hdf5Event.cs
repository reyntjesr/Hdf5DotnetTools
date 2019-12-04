using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Hdf5DotNetTools;

namespace Hdf5DotnetTools.DataTypes
{
    /// <summary>
    /// 
    /// </summary>
    [Hdf5GroupName("Event")]
    [StructLayout(LayoutKind.Sequential)]
    public struct Hdf5Event
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string Event;

        /// <summary>
        /// Time property. Datetimes can't be saved so the TimeTicks field gets saved
        /// </summary>
        [Hdf5Save(Hdf5Save.DoNotSave)]
        public DateTime Time
        {
            get => new DateTime(TimeTicks);
            set => TimeTicks = value.Ticks;
        }

        public long TimeTicks;

        /// <summary>
        /// Duration property. Timespans can't be saved so the DurationTicks field gets saved
        /// </summary>
        [Hdf5Save(Hdf5Save.DoNotSave)]
        public TimeSpan Duration
        {
            get => new TimeSpan(DurationTicks);
            set => DurationTicks = value.Ticks;
        }

        public long DurationTicks;
    }


}
