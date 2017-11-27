using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hdf5DotNetTools;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HDF.PInvoke;
using System.Threading.Tasks;
using System.Threading;

namespace Hdf5UnitTests
{
    public partial class Hdf5UnitTests
    {
        private Hdf5AcquisitionFile FillHeader(Hdf5AcquisitionFile header)
        {
            header.Patient.Name = "Robert";
            header.Patient.Gender = "Male";
            header.Patient.BirthDate = new DateTime(1969, 1, 12);
            header.Patient.Id = "8475805";
            header.Recording.NrOfChannels = 5;
            header.Recording.SampleRate = 200;
            for (int i = 0; i < header.Recording.NrOfChannels; i++)
            {
                var chn = header.Channels[i];
                chn.Label = $"DC{(i + 1):D2}";
                chn.Dimension = "V";
                chn.Offset = 0;
                chn.Amplification = (double)(10 - -10) / (short.MaxValue - short.MinValue);
                chn.SamplingRate = header.Recording.SampleRate;
                header.Channels[i] = chn;
            }
            header.EventList.Add(new Hdf5Event() { Event = "an event", Time = DateTime.Now });
            header.EventList.Add(new Hdf5Event() { Event = "a second event", Time = DateTime.Now + TimeSpan.FromSeconds(2) });
            return header;

        }

        [TestMethod]
        public void WriteAndReadNoDataAcquisitionFile()
        {
            string filename = Path.Combine(folder, "testNoDataAcquisition.H5");
            try
            {
                using (var writer = new Hdf5AcquisitionFileWriter(filename))
                {
                    var data = FillHeader(writer.Header);
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                using (var reader = new Hdf5AcquisitionFileReader(filename))
                {
                    var header = reader.Header;
                    Assert.IsTrue(header.Patient.Name == "Robert");
                    Assert.IsTrue(header.Channels.Select(c => c.Label).SequenceEqual(new string[] { "DC01", "DC02", "DC03", "DC04", "DC05" }));
                    Assert.IsTrue(header.Events.First().Event == "an event");
                    Assert.IsTrue(header.Events.Last().Event == "a second event");
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }

        /// <summary>
        /// an acquisition file is created that has 5 channels of data
        /// for each channel 100 samples are written in two steps of 50 samples to the file
        /// The total number of samples is written to the channels and recording objects
        /// </summary>
        [TestMethod]
        public void WriteAndReadWithSignalsAcquisitionFile()
        {
            string filename = Path.Combine(folder, "testWithSignalsAcquisition.H5");
            try
            {
                using (var writer = new Hdf5AcquisitionFileWriter(filename))
                {
                    var header = FillHeader(writer.Header);
                    var signals = new List<double[]>(header.Recording.NrOfChannels);
                    for (int i = 0; i < header.Recording.NrOfChannels; i++)
                    {
                        signals.Add(Enumerable.Range(i * 50, 50).Select(j => j / 50.0).ToArray());
                    }

                    writer.Write(signals);
                    signals = new List<double[]>(header.Recording.NrOfChannels);
                    for (int i = 0; i < header.Recording.NrOfChannels; i++)
                    {
                        signals.Add(Enumerable.Range((i + 1) * 50, 50).Select(j => j / 50.0).ToArray());
                    }
                    writer.Write(signals);
                    //header.Recording.NrOfSamples = 100;
                    //for (int i = 0; i < header.Channels.Length; i++)
                    //{
                    //    header.Channels[i].NrOfSamples = header.Recording.NrOfSamples;
                    //}
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                using (var reader = new Hdf5AcquisitionFileReader(filename))
                {
                    var header = reader.Header;
                    Assert.IsTrue(header.Patient.Name == "Robert");
                    Assert.IsTrue(header.Recording.NrOfSamples == 100);
                    Assert.IsTrue(header.Channels.Select(c => c.Label).SequenceEqual(new string[] { "DC01", "DC02", "DC03", "DC04", "DC05" }));
                    Assert.IsTrue(header.Channels.Select(c => c.NrOfSamples).SequenceEqual(new ulong[] { 100, 100, 100, 100, 100 }));
                    var data = reader.ReadDouble(0, 49);
                    var sig = data.First().Take(5);
                    Assert.IsTrue(sig.Similar(new double[] { 0, 1 / 50f, 2 / 50f, 3 / 50f, 4 / 50f }));
                    data = reader.ReadDouble(50, 99);
                    sig = data.First().Take(5);
                    Assert.IsTrue(sig.Similar(new double[] { 50 / 50f, 51 / 50f, 52 / 50f, 53 / 50f, 54 / 50f }));
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }

        /// <summary>
        /// an acquisition file is created that has 5 channels of data
        /// for each channel 100 samples are written in two steps of 50 samples to the file
        /// The total number of samples is written to the channels and recording objects
        /// </summary>
        [TestMethod]
        public void WriteAndReadWithDataAcquisitionFile()
        {
            string filename = Path.Combine(folder, "testWithDataAcquisition.H5");
            try
            {
                using (var writer = new Hdf5AcquisitionFileWriter(filename))
                {
                    var header = FillHeader(writer.Header);
                    var data = new short[50, header.Recording.NrOfChannels];
                    for (int i = 0; i < header.Recording.NrOfChannels; i++)
                        for (int j = 0; j < 50; j++)
                        {
                            data[j, i] = writer.Convert2Short(i + j / 50.0, i);
                        }

                    writer.Write(data);
                    for (int i = 0; i < header.Recording.NrOfChannels; i++)
                        for (int j = 0; j < 50; j++)
                        {
                            data[j, i] = writer.Convert2Short(i + 1 + j / 50.0, i);
                        }
                    writer.Write(data);
                    /*header.Recording.NrOfSamples = 100;
                    for (int i = 0; i < header.Channels.Length; i++)
                    {
                        header.Channels[i].NrOfSamples = header.Recording.NrOfSamples;
                    }*/
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                using (var reader = new Hdf5AcquisitionFileReader(filename))
                {
                    var header = reader.Header;
                    Assert.IsTrue(header.Patient.Name == "Robert");
                    Assert.IsTrue(header.Recording.NrOfSamples == 100);
                    Assert.IsTrue(header.Channels.Select(c => c.Label).SequenceEqual(new string[] { "DC01", "DC02", "DC03", "DC04", "DC05" }));
                    Assert.IsTrue(header.Channels.Select(c => c.NrOfSamples).SequenceEqual(new ulong[] { 100, 100, 100, 100, 100 }));
                    var data = reader.ReadDouble(0, 49);
                    var sig = data.First().Take(5);
                    Assert.IsTrue(sig.Similar(new double[] { 0, 1 / 50f, 2 / 50f, 3 / 50f, 4 / 50f }));
                    data = reader.ReadDouble(50, 99);
                    sig = data.First().Take(5);
                    Assert.IsTrue(sig.Similar(new double[] { 50 / 50f, 51 / 50f, 52 / 50f, 53 / 50f, 54 / 50f }));
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }

        /// <summary>
        /// an acquisition file is created that has 5 channels of data
        /// for each channel 100 samples are written in two steps of 50 samples to the file
        /// The total number of samples is written to the channels and recording objects
        /// </summary>
        [TestMethod]
        public void WriteAndReadThreadSafeDataToAcquisitionFile()
        {
            string filename = Path.Combine(folder, "testWithThreadsDataAcquisition.H5");
            try
            {
                using (var writer = new Hdf5AcquisitionFileWriter(filename))
                {
                    var pc = new ProducerConsumer(writer);
                    var header = FillHeader(writer.Header);
                    var data = new List<double[]>();
                    for (int i = 0; i < header.Recording.NrOfChannels; i++)
                    {
                        var row = Enumerable.Range(0, 50).Select(x => i + x / 50.0).ToArray();
                        data.Add(row);
                    }
                    pc.Produce(data);
                    Thread.Sleep(1000);
                    data.Clear();
                    for (int i = 0; i < header.Recording.NrOfChannels; i++)
                    {
                        var row = Enumerable.Range(0, 50).Select(x => i + 1 + x / 50.0).ToArray();
                        data.Add(row);
                    }
                    pc.Produce(data);
                    Thread.Sleep(1000);
                    pc.Done();
                    /*header.Recording.NrOfSamples = 100;
                    for (int i = 0; i < header.Channels.Length; i++)
                    {
                        header.Channels[i].NrOfSamples = header.Recording.NrOfSamples;
                    }*/
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                using (var reader = new Hdf5AcquisitionFileReader(filename))
                {
                    var header = reader.Header;
                    Assert.IsTrue(header.Patient.Name == "Robert");
                    Assert.IsTrue(header.Recording.NrOfSamples == 100);
                    Assert.IsTrue(header.Channels.Select(c => c.Label).SequenceEqual(new string[] { "DC01", "DC02", "DC03", "DC04", "DC05" }));
                    Assert.IsTrue(header.Channels.Select(c => c.NrOfSamples).SequenceEqual(new ulong[] { 100, 100, 100, 100, 100 }));
                    var data = reader.ReadDouble(0, 49);
                    var sig = data.First().Take(5);
                    Assert.IsTrue(sig.Similar(new double[] { 0, 1 / 50f, 2 / 50f, 3 / 50f, 4 / 50f }));
                    data = reader.ReadDouble(50, 99);
                    sig = data.First().Take(5);
                    Assert.IsTrue(sig.Similar(new double[] { 50 / 50f, 51 / 50f, 52 / 50f, 53 / 50f, 54 / 50f }));
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }

        //[TestMethod]
        //public void ReadAcquisitionFile()
        //{
        //    var filename = Path.Combine(@"D:\Matlab\Data\Maryam\OH", "FA00101O.H5");
        //    using (var reader = new Hdf5AcquisitionFileReader(filename))
        //    {
        //        var header = reader.Header;
        //        var data = reader.Read(0,1000);
        //    }
        //}
    }
}
