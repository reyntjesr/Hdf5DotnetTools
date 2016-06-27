using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hdf5DotNetTools;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HDF.PInvoke;
using System.Reflection;

namespace Hdf5UnitTests
{
    public partial class Hdf5UnitTests
    {
        [TestMethod]
        public void WriteAndReadDataset()
        {
            string filename = Path.Combine(folder, "testDataset.H5");
            var dset = dsets.First();

            try
            {
                int fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                Hdf5.WriteDataset(fileId, "/test", dset);
                Hdf5.CloseFile(fileId);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                int fileId = Hdf5.OpenFile(filename);
                Assert.IsTrue(fileId > 0);
                double[,] dset2 = Hdf5.ReadDataset<double>(fileId, "/test");
                compareDatasets(dset, dset2);
                bool same = dset == dset2;

                Hdf5.CloseFile(fileId);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }

        [TestMethod]
        public void WriteAndReadPrimitives()
        {
            string filename = Path.Combine(folder, "testDataset.H5");
            int intValue = 2;
            double dblValue = 1.1;
            string strValue = "test";
            bool boolValue = true;
            var groupStr = "/test";
            Func<string, string> concatFunc = (x) => string.Concat(groupStr, "/", x);

            try
            {
                int fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                int groupId = Hdf5.CreateGroup(fileId, groupStr);
                Hdf5.WritePrimitive(groupId, concatFunc(nameof(intValue)), intValue);
                Hdf5.WritePrimitive(groupId, concatFunc(nameof(dblValue)), dblValue);
                Hdf5.WritePrimitive(groupId, concatFunc(nameof(strValue)), strValue);
                Hdf5.WritePrimitive(groupId, concatFunc(nameof(boolValue)), boolValue);
                H5G.close(groupId);
                Hdf5.CloseFile(fileId);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                int fileId = Hdf5.OpenFile(filename);
                Assert.IsTrue(fileId > 0);
                int groupId = H5G.open(fileId, groupStr);
                int readInt = Hdf5.ReadPrimitive<int>(groupId, concatFunc(nameof(intValue)));
                Assert.IsTrue(intValue == readInt);
                double readDbl = Hdf5.ReadPrimitive<double>(groupId, concatFunc(nameof(dblValue)));
                Assert.IsTrue(dblValue == readDbl);
                string readStr = Hdf5.ReadPrimitive<string>(groupId, concatFunc(nameof(strValue)));
                Assert.IsTrue(strValue == readStr);
                bool readBool = Hdf5.ReadPrimitive<bool>(groupId, concatFunc(nameof(boolValue)));
                Assert.IsTrue(boolValue == readBool);
                H5G.close(groupId);
                Hdf5.CloseFile(fileId);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }

        [TestMethod]
        public void WriteAndReadAllPrimitives()
        {

            string filename = Path.Combine(folder, "testAllPrimitives.H5");
            var groupStr = "/test";
            Func<string, string> concatFunc = (x) => string.Concat(groupStr, "/", x);
            try
            {
                int fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                Hdf5.WriteObject(fileId, allTypesObject, "/test");

                var readObject = Hdf5.ReadObject<AllTypesClass>(fileId, "/test");
                Assert.IsTrue(allTypesObject.PublicInstanceFieldsEqual(readObject));
                Assert.IsTrue(Hdf5.CloseFile(fileId) == 0);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }

        [TestMethod]
        public void WriteAndReadChunckedDataset()
        {
            string filename = Path.Combine(folder, "testChunks.H5");

            try
            {
                int fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                var chunkSize = new ulong[] { 5, 5 };
                using (var chunkedDset = new ChunkedDataset<double>("/test", fileId, dsets.First()))
                {
                    foreach (var ds in dsets.Skip(1))
                    {
                        chunkedDset.AppendDataset(ds);
                    };

                }
                Hdf5.CloseFile(fileId);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                int fileId = Hdf5.OpenFile(filename);
                var dset = Hdf5.ReadDataset<double>(fileId, "/test");


                Assert.IsTrue(dset.Rank == dsets.First().Rank);
                var xSum = dsets.Select(d => d.GetLength(0)).Sum();
                Assert.IsTrue(xSum == dset.GetLength(0));
                var testRange = Enumerable.Range(0, 30).Select(t => (double)t);

                // get every 5th element in the matrix
                var x0Range = dset.Cast<double>().Where((d, i) => i % 5 == 0);
                Assert.IsTrue(testRange.SequenceEqual(x0Range));

                Hdf5.CloseFile(fileId);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }

        [TestMethod]
        public void WriteAndReadSubsetOfDataset()
        {
            string filename = Path.Combine(folder, "testSubset.H5");
            try
            {
                int fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                var chunkSize = new ulong[] { 5, 5 };
                using (var chunkedDset = new ChunkedDataset<double>("/test", fileId, dsets.First()))
                {
                    foreach (var ds in dsets.Skip(1))
                    {
                        chunkedDset.AppendDataset(ds);
                    };

                }

                Hdf5.CloseFile(fileId);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                int fileId = Hdf5.OpenFile(filename);
                ulong begIndex = 8;
                ulong endIndex = 21;
                var dset = Hdf5.ReadDataset<double>(fileId, "/test", begIndex, endIndex);
                Hdf5.CloseFile(fileId);


                Assert.IsTrue(dset.Rank == dsets.First().Rank);
                int count = Convert.ToInt32(endIndex - begIndex);
                Assert.IsTrue(count == dset.GetLength(0));
                // Creat a range from number 8 to 21
                var testRange = Enumerable.Range((int)begIndex, count).Select(t => (double)t);

                // Get the first column from row index number 8 (the 9th row) to row index number 21 (22th row) 
                var x0Range = dset.Cast<double>().Where((d, i) => i % 5 == 0);
                Assert.IsTrue(testRange.SequenceEqual(x0Range));
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

        }

    }
}
