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
        [TestMethod]
        public void WriteAndReadAttribute()
        {
            string filename = Path.Combine("testAttribute.H5");
            try
            {
                var fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                var groupId = Hdf5.CreateGroup(fileId, "root");
                DateTime nowTime = DateTime.Now;
                Hdf5.WriteAttribute(groupId, "time", nowTime);
                DateTime readTime = Hdf5.ReadAttribute<DateTime>(groupId, "time");
                Assert.IsTrue(readTime == nowTime);
                Assert.IsTrue(Hdf5.CloseFile(fileId) == 0);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }
    }
}
