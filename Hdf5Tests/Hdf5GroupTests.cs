using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hdf5DotNetTools;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HDF.PInvoke;

namespace Hdf5UnitTests
{
    public partial class Hdf5UnitTests
    {
        [TestMethod]
        public void WriteAndReadGroupsWithDataset()
        {
            string filename = Path.Combine(folder, "testGroups.H5");

            try
            {
                int fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                var dset = dsets.First();

                int groupId = H5G.create(fileId, "/A"); ///B/C/D/E/F/G/H
                Hdf5.WriteDataset(groupId, "test", dset);
                int subGroupId = Hdf5.CreateGroup(groupId, "C");
                dset = dsets.Skip(1).First();
                Hdf5.WriteDataset(subGroupId, "test2", dset);
                Hdf5.CloseGroup(subGroupId);
                Hdf5.CloseGroup(groupId);
                groupId = H5G.create(fileId, "/A/B"); ///B/C/D/E/F/G/H
                dset = dsets.Skip(1).First();
                Hdf5.WriteDataset(groupId, "test", dset);
                Hdf5.CloseGroup(groupId);

                groupId = Hdf5.CreateGroupRecursively(fileId, "A/B/C/D/E/F/I");
                Hdf5.CloseGroup(groupId);
                Hdf5.CloseFile(fileId);


                fileId = Hdf5.OpenFile(filename);
                Assert.IsTrue(fileId > 0);
                groupId = H5G.open(fileId, "/A/B");
                double[,] dset2 = Hdf5.ReadDataset<double>(groupId, "test");
                compareDatasets(dset, dset2);
                Assert.IsTrue(Hdf5.CloseGroup(groupId) >= 0);
                groupId = H5G.open(fileId, "/A/C");
                dset2 = Hdf5.ReadDataset<double>(groupId, "test2");
                compareDatasets(dset, dset2);
                Assert.IsTrue(Hdf5.CloseGroup(groupId) >= 0);
                bool same = dset == dset2;
                dset = dsets.First();
                dset2 = Hdf5.ReadDataset<double>(fileId, "/A/test");
                compareDatasets(dset, dset2);
                Assert.IsTrue(Hdf5.GroupExists(fileId, "A/B/C/D/E/F/I"));

                Assert.IsTrue(Hdf5.CloseFile(fileId) == 0);

            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }
    }
}
