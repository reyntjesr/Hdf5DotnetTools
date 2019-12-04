using System;
using System.Collections.Generic;
using System.Text;
using Hdf5DotNetTools;

namespace Hdf5DotnetTools.DataTypes
{
    [Hdf5GroupName("Patient")]
    public class Hdf5Patient
    {
        public string Name = "";
        public string Id = "";
        public int RecId = -1;
        public string Gender = "";
        public DateTime BirthDate = DateTime.Now;
        public double Height = double.NaN;
        public double Weight = double.NaN;
        public DateTime EditData = DateTime.Now;
    }
}
