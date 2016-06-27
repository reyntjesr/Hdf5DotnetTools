using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Hdf5UnitTests
{
    [TestClass]
    public partial class Hdf5UnitTests
    {

        class AllTypesClass
        {
            public Boolean aBool = true;
            public Byte aByte = 10;
            public Char aChar = 'a';
            public DateTime aDatetime = new DateTime(1969, 12, 01, 12, 00, 00, DateTimeKind.Local);
            public Decimal aDecimal = new decimal(2.344);
            public Double aDouble = 2.1;
            public Int16 aInt16 = 10;
            public Int32 aInt32 = 100;
            public Int64 aInt64 = 1000;
            public SByte aSByte = 10;
            public Single aSingle = 100;
            public UInt16 aUInt16 = 10;
            public UInt32 aUInt32 = 100;
            public UInt64 aUInt64 = 1000;
            public String aString = "test";
            public TimeSpan aTimeSpan = TimeSpan.FromHours(1);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct wData
        {
            public int serial_no;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
            public string location;
            public double temperature;
            public double pressure;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct wData2
        {
            public int serial_no;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
            public string location;
            public double temperature;
            public double pressure;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)]
            public string label;
        }
        private class TestClass : IEquatable<TestClass>
        {
            public int TestInteger { get; set; }
            public double TestDouble { get; set; }
            public bool TestBoolean { get; set; }
            public string TestString { get; set; }

            public bool Equals(TestClass other)
            {
                return other.TestInteger == TestInteger &&
            other.TestDouble == TestDouble &&
            other.TestBoolean == TestBoolean &&
            other.TestString == TestString;
            }
        }
        private class TestClassWithArray : TestClass
        {
            public double[] TestDoubles { get; set; }
            public string[] TestStrings { get; set; }

            public bool Equals(TestClassWithArray other)
            {
                return base.Equals(other) &&
                    other.TestDoubles.SequenceEqual(TestDoubles) &&
                    other.TestStrings.SequenceEqual(TestStrings);

            }
        }

        static private TestClass testClass;
        static private TestClassWithArray testClassWithArrays;
        static private List<double[,]> dsets;
        static private wData[] wDataList;
        static private wData2[] wData2List;
        static private AllTypesClass allTypesObject;

        static private string folder;

        [ClassInitialize()]
        public static void ClassInitialize(TestContext context)
        {
            //folder = System.IO.Path.GetTempPath();
            folder = AppDomain.CurrentDomain.BaseDirectory;
            dsets = new List<double[,]> {
                createDataset(),
                createDataset(10),
                createDataset(20) };

            wDataList = new wData[4] {
                new wData() { serial_no = 1153, location = "Exterior (static)", temperature = 53.23, pressure = 24.57 },
                new wData() { serial_no = 1184, location = "Intake",  temperature = 55.12, pressure = 22.95 },
                new wData() { serial_no = 1027, location = "Intake manifold", temperature = 103.55, pressure = 31.23 },
                new wData() { serial_no = 1313, location = "Exhaust manifold", temperature = 1252.89, pressure = 84.11 }
            };

            wData2List = new wData2[4] {
                new wData2() { serial_no = 1153, location = "Exterior (static)", label="V",temperature = 53.23, pressure = 24.57 },
                new wData2() { serial_no = 1184, location = "Intake", label="uV", temperature = 55.12, pressure = 22.95 },
                new wData2() { serial_no = 1027, location = "Intake manifold", label="V",temperature = 103.55, pressure = 31.23 },
                new wData2() { serial_no = 1313, location = "Exhaust manifold", label="mV", temperature = 1252.89, pressure = 84.11 }
            };

            testClass = new TestClass();
            testClassWithArrays = new TestClassWithArray();
            allTypesObject = new AllTypesClass();

            var files = Directory.GetFiles(folder, "*.H5");
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                {

                    throw;
                }

            }
        }

        /// <summary>
        /// create a matrix and fill it with numbers
        /// </summary>
        /// <param name="offset"></param>
        /// <returns>the matrix </returns>
        private static double[,] createDataset(int offset = 0)
        {
            var dset = new double[10, 5];
            for (var i = 0; i < 10; i++)
                for (var j = 0; j < 5; j++)
                {
                    double x = i + j * 5 + offset;
                    dset[i, j] = (j == 0) ? x : x / 10;
                }
            return dset;
        }


        private static void compareDatasets(double[,] dset, double[,] dset2)
        {
            Assert.IsTrue(dset.Rank == dset2.Rank);
            Assert.IsTrue(
                Enumerable.Range(0, dset.Rank).All(dimension =>
                dset.GetLength(dimension) == dset2.GetLength(dimension)));
            Assert.IsTrue(dset.Cast<double>().SequenceEqual(dset2.Cast<double>()));
        }

        private void CreateExceptionAssert(Exception ex)
        {
            Console.WriteLine(ex.ToString());
            var failStr = "Unexpected exception of type {0} caught: {1}";
            failStr = string.Format(failStr, ex.GetType(), ex.Message);
            Assert.Fail(failStr);

        }
    }

}