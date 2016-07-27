using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Hdf5DotNetTools;
using System.IO;

namespace Hdf5UnitTests
{
    public partial class Hdf5UnitTests
    {


        [TestMethod]
        public void WriteAndReadStructs()
        {
            string filename = Path.Combine(folder, "testCompounds.H5");

            try
            {

                var fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                var status = Hdf5.WriteCompounds(fileId, "/test", wDataList);
                Hdf5.CloseFile(fileId);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                var fileId = Hdf5.OpenFile(filename);
                Assert.IsTrue(fileId > 0);
                var cmpList = Hdf5.ReadCompounds<wData>(fileId, "/test").ToArray();
                Hdf5.CloseFile(fileId);
                CollectionAssert.AreEqual(wDataList,cmpList);

            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

        }

    }
}
