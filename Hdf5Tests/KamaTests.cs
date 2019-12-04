using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hdf5DotNetTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hdf5UnitTests
{
    public class KamaTests
    {
        private string folder = AppDomain.CurrentDomain.BaseDirectory;
        [TestMethod]
        public void CreateFile()
        {
            //string filename = Path.Combine(folder, "Kama.H5");
            //try
            //{
            //    using (var writer = new Hdf5AcquisitionFileWriter(filename))
            //    {
            //        var header = null;
            //        var signals = new List<double[]>(header.Recording.NrOfChannels);
            //        for (int i = 0; i < header.Recording.NrOfChannels; i++)
            //        {
            //            signals.Add(Enumerable.Range(i * 50, 50).Select(j => j / 50.0).ToArray());
            //        }

            //        writer.Write(signals);
            //        signals = new List<double[]>(header.Recording.NrOfChannels);
            //        for (int i = 0; i < header.Recording.NrOfChannels; i++)
            //        {
            //            signals.Add(Enumerable.Range((i + 1) * 50, 50).Select(j => j / 50.0).ToArray());
            //        }
            //        writer.Write(signals);
            //        //header.Recording.NrOfSamples = 100;
            //        //for (int i = 0; i < header.Channels.Length; i++)
            //        //{
            //        //    header.Channels[i].NrOfSamples = header.Recording.NrOfSamples;
            //        //}
            //    }
            //}
            //catch (Exception ex)
            //{
            //    //CreateExceptionAssert(ex);
            //}

        }
    }
}
