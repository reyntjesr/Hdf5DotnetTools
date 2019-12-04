using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hdf5DotNetTools;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HDF.PInvoke;
using System.Threading.Tasks;
using System.Threading;
using Hdf5DotnetTools.DataTypes;

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
                    Assert.IsTrue(header.Events.Events.First() == "an event");
                    Assert.IsTrue(header.Events.Events.Last() == "a second event");
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
            // File Delete gives exception
            //try
            //{
            //    File.Delete(filename); // Cannot delete the file. Error being used by another process.

            //}
            //catch (Exception ex)
            //{
            //    CreateExceptionAssert(ex);
            //}
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
            int N = 50;
            int nrNsamples = 1000;
            try
            {
                using (var writer = new Hdf5AcquisitionFileWriter(filename))
                {
                    void saveChunck(IEnumerable<double[]> d)
                    {
                        writer.Write(d);
                    }
                    var pc = new DataProducerConsumer<IEnumerable<double[]>>(saveChunck);
                    var header = FillHeader(writer.Header);
                    var data = new List<double[]>();

                    for (int j = 0; j < nrNsamples; j++)
                    {

                        for (int i = 0; i < header.Recording.NrOfChannels; i++)
                        {
                            var row = Enumerable.Range(0, N).Select(x => i + j + x / (double)N).ToArray();
                            data.Add(row);
                        }
                        pc.Produce(data);
                        Thread.Sleep(10);
                        data.Clear();
                    }
                    //for (int i = 0; i < header.Recording.NrOfChannels; i++)
                    //{
                    //    var row = Enumerable.Range(0, 50).Select(x => i + 1 + x / 50.0).ToArray();
                    //    data.Add(row);
                    //}
                    //pc.Produce(data);
                    Thread.Sleep(1000);
                    pc.Done();
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
                    ulong samps = Convert.ToUInt64(N * nrNsamples);
                    Assert.IsTrue(header.Recording.NrOfSamples == samps);
                    Assert.IsTrue(header.Channels.Select(c => c.Label).SequenceEqual(new string[] { "DC01", "DC02", "DC03", "DC04", "DC05" }));
                    Assert.IsTrue(header.Channels.Select(c => c.NrOfSamples).SequenceEqual(new ulong[] { samps, samps, samps, samps, samps }));
                    var data = reader.ReadDouble(0, (ulong)N - 1);
                    var sig = data.First().Take(5);
                    var sim = Enumerable.Range(0, 5).Select(d => d / (double)N);
                    Assert.IsTrue(sig.Similar(sim));
                    data = reader.ReadDouble((ulong)N, Convert.ToUInt64(2 * N - 1));
                    sig = data.First().Take(5);
                    sim = Enumerable.Range(N, 5).Select(d => d / (double)N);
                    Assert.IsTrue(sig.Similar(sim));
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
        public void WriteAndReadShortThreadSafeDataToAcquisitionFile()
        {
            string filename = Path.Combine(folder, "testWithThreadsDataAcquisition.H5");
            int N = 50;
            int nrNsamples = 1000;
            try
            {
                using (var writer = new Hdf5AcquisitionFileWriter(filename))
                {
                    void saveChunck(short[,] d)
                    {
                        writer.Write(d);
                    }
                    var pc = new DataProducerConsumer<short[,]>(saveChunck);
                    var header = FillHeader(writer.Header);
                    var data = new List<double[]>();

                    for (int j = 0; j < nrNsamples; j++)
                    {

                        short[,] shData = new short[N, header.Recording.NrOfChannels];
                        for (int i = 0; i < header.Recording.NrOfChannels; i++)
                        {
                            var row = Enumerable.Range(0, N).Select(x => i + j + x / (double)N).ToArray();
                            data.Add(row);
                            for (int k = 0; k < N; k++)
                            {
                                shData[k, i] = writer.Convert2Short(row[k], i);
                            }
                        }

                        pc.Produce(shData);
                        Thread.Sleep(10);
                        data.Clear();
                    }
                    //for (int i = 0; i < header.Recording.NrOfChannels; i++)
                    //{
                    //    var row = Enumerable.Range(0, 50).Select(x => i + 1 + x / 50.0).ToArray();
                    //    data.Add(row);
                    //}
                    //pc.Produce(data);
                    Thread.Sleep(1000);
                    pc.Done();
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
                    ulong samps = Convert.ToUInt64(N * nrNsamples);
                    Assert.IsTrue(header.Recording.NrOfSamples == samps);
                    Assert.IsTrue(header.Channels.Select(c => c.Label).SequenceEqual(new string[] { "DC01", "DC02", "DC03", "DC04", "DC05" }));
                    Assert.IsTrue(header.Channels.Select(c => c.NrOfSamples).SequenceEqual(new ulong[] { samps, samps, samps, samps, samps }));
                    var data = reader.ReadDouble(0, (ulong)N - 1);
                    var sig = data.First().Take(5);
                    var sim = Enumerable.Range(0, 5).Select(d => d / (double)N);
                    Assert.IsTrue(sig.Similar(sim));
                    data = reader.ReadDouble((ulong)N, Convert.ToUInt64(2 * N - 1));
                    sig = data.First().Take(5);
                    sim = Enumerable.Range(N, 5).Select(d => d / (double)N);
                    Assert.IsTrue(sig.Similar(sim));
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
        public void WriteAndReadWithCloseThreadSafeDataToAcquisitionFile()
        {
            string filename = Path.Combine(folder, "testWithThreadsDataAcquisition.H5");
            int N = 50;
            int nrNsamples = 1000;
            try
            {
                var writer = new Hdf5AcquisitionFileWriter(filename);
                void SaveChunck(IEnumerable<double[]> d)
                {
                    writer.Write(d);
                }
                var pc = new DataProducerConsumer<IEnumerable<double[]>>(SaveChunck);
                var header = FillHeader(writer.Header);
                var data = new List<double[]>();

                for (int j = 0; j < nrNsamples; j++)
                {

                    for (int i = 0; i < header.Recording.NrOfChannels; i++)
                    {
                        var row = Enumerable.Range(0, N).Select(x => i + j + x / (double)N).ToArray();
                        data.Add(row);
                    }
                    pc.Produce(data);
                    Thread.Sleep(10);
                    data.Clear();
                }
                //for (int i = 0; i < header.Recording.NrOfChannels; i++)
                //{
                //    var row = Enumerable.Range(0, 50).Select(x => i + 1 + x / 50.0).ToArray();
                //    data.Add(row);
                //}
                //pc.Produce(data);
                Thread.Sleep(1000);
                pc.Done();
                writer.Dispose();
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
                    ulong samps = Convert.ToUInt64(N * nrNsamples);
                    Assert.IsTrue(header.Recording.NrOfSamples == samps);
                    Assert.IsTrue(header.Channels.Select(c => c.Label).SequenceEqual(new string[] { "DC01", "DC02", "DC03", "DC04", "DC05" }));
                    Assert.IsTrue(header.Channels.Select(c => c.NrOfSamples).SequenceEqual(new ulong[] { samps, samps, samps, samps, samps }));
                    var data = reader.ReadDouble(0, (ulong)N - 1);
                    var sig = data.First().Take(5);
                    var sim = Enumerable.Range(0, 5).Select(d => d / (double)N);
                    Assert.IsTrue(sig.Similar(sim));
                    data = reader.ReadDouble((ulong)N, Convert.ToUInt64(2 * N - 1));
                    sig = data.First().Take(5);
                    sim = Enumerable.Range(N, 5).Select(d => d / (double)N);
                    Assert.IsTrue(sig.Similar(sim));
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

        }

    }
}
