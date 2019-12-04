using System;
using System.Collections.Generic;
using System.Text;

namespace Hdf5DotnetTools.DataTypes
{
    public class FileClosedArgs : EventArgs
    {
        public string ClosedFile { get; }
        public bool CancelRequested { get; set; }

        public FileClosedArgs(string fileName)
        {
            ClosedFile = fileName;
        }
    }
}
