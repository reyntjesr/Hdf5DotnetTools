using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Hdf5DotNetTools;

namespace Hdf5UnitTests
{
    [TestClass]
    public partial class Hdf5UnitTests
    {
        [Hdf5Attributes(new string[] { "some info", "more info" })]
        class AttributeClass
        {
            public class NestedInfo
            {
                public int noAttribute = 10;

                [Hdf5Attribute("some money")]
                public decimal money = 100.12M;
            }

            [Hdf5Attribute("birthdate")]
            public DateTime aDatetime = new DateTime(1969, 12, 01, 12, 00, 00, DateTimeKind.Local);

            public double noAttribute = 10.0;

            public NestedInfo nested = new NestedInfo();
        }

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
        private struct WData
        {
            public int serial_no;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
            public string location;
            public double temperature;
            public double pressure;

            public DateTime Time
            {
                get { return new DateTime(timeTicks); }
                set
                {
                    timeTicks = value.Ticks;
                }
            }

            public long timeTicks;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WData2
        {
            public int serial_no;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
            public string location;
            public double temperature;
            public double pressure;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)]
            public string label;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Responses
        {
            public Int64 MCID;
            public int PanelIdx;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public short[] ResponseValues;
        }

        private class TestClass : IEquatable<TestClass>
        {
            public int TestInteger { get; set; }
            public double TestDouble { get; set; }
            public bool TestBoolean { get; set; }
            public string TestString { get; set; }

            public DateTime TestTime { get; set; }

            public bool Equals(TestClass other)
            {
                return other.TestInteger == TestInteger &&
            other.TestDouble == TestDouble &&
            other.TestBoolean == TestBoolean &&
                        other.TestString == TestString &&
                other.TestTime == TestTime;
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

        class TestClassWithStructs
        {
            public TestClassWithStructs()
            {
            }
            public WData[] DataList { get; set; }
        }

        static private TestClass testClass;
        static private TestClassWithArray testClassWithArrays;
        static private List<double[,]> dsets;
        static private WData[] wDataList;
        static private WData2[] wData2List;
        static private Responses[] responseList;
        static private AllTypesClass allTypesObject;
        static private TestClassWithStructs classWithStructs;

        static private string folder;

        [ClassInitialize()]
        public static void ClassInitialize(TestContext context)
        {
            //folder = System.IO.Path.GetTempPath();
            folder = AppDomain.CurrentDomain.BaseDirectory;
            dsets = new List<double[,]> {
                CreateDataset(),
                CreateDataset(10),
                CreateDataset(20) };

            wDataList = new WData[4] {
                new WData() { serial_no = 1153, location = "Exterior (static)", temperature = 53.23, pressure = 24.57, Time=new DateTime(2000,1,1) },
                new WData() { serial_no = 1184, location = "Intake",  temperature = 55.12, pressure = 22.95, Time=new DateTime(2000,1,2) },
                new WData() { serial_no = 1027, location = "Intake manifold", temperature = 103.55, pressure = 31.23, Time=new DateTime(2000,1,3) },
                new WData() { serial_no = 1313, location = "Exhaust manifold", temperature = 1252.89, pressure = 84.11, Time=new DateTime(2000,1,4) }
            };

            wData2List = new WData2[4] {
                new WData2() { serial_no = 1153, location = "Exterior (static)", label="V",temperature = 53.23, pressure = 24.57 },
                new WData2() { serial_no = 1184, location = "Intake", label="uV", temperature = 55.12, pressure = 22.95 },
                new WData2() { serial_no = 1027, location = "Intake manifold", label="V",temperature = 103.55, pressure = 31.23 },
                new WData2() { serial_no = 1313, location = "Exhaust manifold", label="mV", temperature = 1252.89, pressure = 84.11 }
            };
            responseList = new Responses[4] {
                new Responses() { MCID=1,PanelIdx=5,ResponseValues=new short[4]{ 1,2,3,4} },
                new Responses() { MCID=2,PanelIdx=6,ResponseValues=new short[4]{ 5,6,7,8}},
                new Responses() { MCID=3,PanelIdx=7,ResponseValues=new short[4]{ 1,2,3,4}},
                new Responses() { MCID=4,PanelIdx=8,ResponseValues=new short[4]{ 5,6,7,8}}
            };

            classWithStructs = new TestClassWithStructs { DataList = wDataList };
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
        private static double[,] CreateDataset(int offset = 0)
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


        private static void CompareDatasets<T>(T[,] dset, T[,] dset2)
        {
            Assert.IsTrue(dset.Rank == dset2.Rank);
            Assert.IsTrue(
                Enumerable.Range(0, dset.Rank).All(dimension =>
                dset.GetLength(dimension) == dset2.GetLength(dimension)));
            Assert.IsTrue(dset.Cast<T>().SequenceEqual(dset2.Cast<T>()));
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