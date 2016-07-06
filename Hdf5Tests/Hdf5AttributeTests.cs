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
        public void WriteAndReadAttribute()
        {
            string filename = Path.Combine(folder, "testAttribute.H5");
            try
            {
                int fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                int groupId = Hdf5.CreateGroup(fileId, "test");
                DateTime nowTime = DateTime.Now;
                Hdf5.WriteAttribute(groupId, "time", nowTime);
                DateTime readTime = Hdf5.ReadAttribute<DateTime>(groupId, "time");
                Assert.IsTrue(readTime == nowTime);
                Assert.IsTrue(Hdf5.CloseFile(fileId)==0);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }

        [TestMethod]
        public void WriteAndReadStringAttribute()
        {
            string filename = Path.Combine(folder, "testAttributeString.H5");
            try
            {
                int fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                int groupId = Hdf5.CreateGroup(fileId, "test");
                string attrStr = "this is an attribute";
                Hdf5.WriteAttribute(groupId, "time", attrStr);
                string readStr= Hdf5.ReadAttribute<string>(groupId, "time");
                Assert.IsTrue(readStr == attrStr);
                Assert.IsTrue(Hdf5.CloseFile(fileId) == 0);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }

        [TestMethod]
        public void WriteAndReadAttributes()
        {
            string filename = Path.Combine(folder, "testAttributes.H5");
            int[] intValues = new int[] { 1, 2 };
            double dblValue = 1.1;
            string strValue = "test";
            string[] strValues = new string[2] { "test", "another test" };
            bool boolValue = true;
            var groupStr = "/test";
            Func<string, string> concatFunc = (x) => string.Concat(groupStr, "/", x);
            string intName = nameof(intValues);
            string dblName = nameof(dblValue);
            string strName = nameof(strValue);
            string strNames = nameof(strValues);
            string boolName = nameof(boolValue);

            try
            {
                int fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                int groupId = Hdf5.CreateGroup(fileId, groupStr);
                Hdf5.WriteAttributes<int>(groupId, intName, intValues);
                Hdf5.WriteAttribute(groupId, dblName, dblValue);
                Hdf5.WriteAttribute(groupId, strName, strValue);
                Hdf5.WriteAttributes<string>(groupId, strNames, strValues);
                Hdf5.WriteAttribute(groupId, boolName, boolValue);
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
                IEnumerable<int> readInts = (int[])Hdf5.ReadAttributes<int>(groupId, intName);
                Assert.IsTrue(intValues.SequenceEqual(readInts));
                double readDbl = Hdf5.ReadAttribute<double>(groupId, dblName);
                Assert.IsTrue(dblValue == readDbl);
                string readStr = Hdf5.ReadAttribute<string>(groupId, strName);
                Assert.IsTrue(strValue == readStr);
                IEnumerable<string> readStrs = (string[])Hdf5.ReadAttributes<string>(groupId, strNames);
                Assert.IsTrue(strValues.SequenceEqual(readStrs));
                bool readBool = Hdf5.ReadAttribute<bool>(groupId, boolName);
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
        public void WriteAndReadObjectWithHdf5Attributes()
        {
            string filename = Path.Combine(folder, "testHdf5Attribute.H5");
            var attObject = new AttributeClass();
            try
            {
                int fileId = Hdf5.CreateFile(filename);
                Assert.IsTrue(fileId > 0);
                Hdf5.WriteObject(fileId, attObject,"anObject");
                Assert.IsTrue(Hdf5.CloseFile(fileId) == 0);
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }
    }
}
