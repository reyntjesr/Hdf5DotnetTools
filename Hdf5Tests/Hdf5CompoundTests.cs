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
        public void WriteAndReadObjectWithStructs()
        {
            string filename = Path.Combine(folder, "testObjectWithStructArray.H5");


            try
            {

                var fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                var status = Hdf5.WriteObject(fileId, classWithStructs, "test");
                Hdf5.CloseFile(fileId);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                TestClassWithStructs objWithStructs;
                var fileId = Hdf5.OpenFile(filename);
                Assert.IsTrue(fileId > 0);
                objWithStructs = Hdf5.ReadObject<TestClassWithStructs>(fileId, "test");
                CollectionAssert.AreEqual(wDataList, objWithStructs.DataList);
                Hdf5.CloseFile(fileId);


            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

        }



        [TestMethod]
        public void WriteAndReadStructs()
        {
            string filename = Path.Combine(folder, "testCompounds.H5");

            try
            {

                var fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                var status = Hdf5.WriteCompounds(fileId, "/test", wData2List);
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
                var cmpList = Hdf5.ReadCompounds<WData2>(fileId, "/test").ToArray();
                Hdf5.CloseFile(fileId);
                CollectionAssert.AreEqual(wData2List, cmpList);

            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

        }

        [TestMethod]
        public void WriteAndReadStructsWithDatetime()
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
                var cmpList = Hdf5.ReadCompounds<WData>(fileId, "/test").ToArray();
                Hdf5.CloseFile(fileId);
                CollectionAssert.AreEqual(wDataList, cmpList);

            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

        }

        [TestMethod]
        public void WriteAndReadStructsWithArray()
        {
            string filename = Path.Combine(folder, "testArrayCompounds.H5");

            try
            {

                var fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                var status = Hdf5.WriteCompounds(fileId, "/test", responseList);
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
                Responses[] cmpList = Hdf5.ReadCompounds<Responses>(fileId, "/test").ToArray();
                Hdf5.CloseFile(fileId);
                var isSame = responseList.Zip(cmpList, (r, c) =>
                {
                    return r.MCID == c.MCID &&
                    r.PanelIdx == c.PanelIdx &&
                    r.ResponseValues.Zip(c.ResponseValues, (rr, cr) => rr == cr).All(v => v == true);
                });
                Assert.IsTrue(isSame.All(s => s == true));

            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

        }

    }
}
