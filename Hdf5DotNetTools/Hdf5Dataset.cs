using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HDF.PInvoke;
using System.Runtime.InteropServices;
using System.IO;

namespace Hdf5DotNetTools
{
#if HDF5_VER1_10
    using hid_t = System.Int64;
#else
    using hid_t = System.Int32;
#endif

    public static partial class Hdf5
    {
        static Hdf5ReaderWriter dsetRW = new Hdf5ReaderWriter(new Hdf5Dataset());

        /// <summary>
        /// Reads an n-dimensional dataset.
        /// </summary>
        /// <typeparam name="T">Generic parameter strings or primitive type</typeparam>
        /// <param name="groupId">id of the group. Can also be a file Id</param>
        /// <param name="name">name of the dataset</param>
        /// <returns>The n-dimensional dataset</returns>
        public static Array ReadDatasetToArray<T>(hid_t groupId, string name) //where T : struct
        {
            var datatype = GetDatatype(typeof(T));


            var datasetId = H5D.open(groupId, name);
            var spaceId = H5D.get_space(datasetId);
            int rank = H5S.get_simple_extent_ndims(spaceId);
            long count = H5S.get_simple_extent_npoints(spaceId);
            Array dset;
            Type type = typeof(T);
            if (rank >= 0 && count >= 0)
            {
                int rankChunk;
                ulong[] maxDims = new ulong[rank];
                ulong[] dims = new ulong[rank];
                ulong[] chunkDims = new ulong[rank];
                hid_t memId = H5S.get_simple_extent_dims(spaceId, dims, maxDims);
                long[] lengths = dims.Select(d => Convert.ToInt64(d)).ToArray();
                dset = Array.CreateInstance(type, lengths);
                var typeId = H5D.get_type(datasetId);
                var mem_type = H5T.copy(datatype);
                if (datatype == H5T.C_S1)
                    H5T.set_size(datatype, new IntPtr(2));

                var propId = H5D.get_create_plist(datasetId);

                if (H5D.layout_t.CHUNKED == H5P.get_layout(propId))
                    rankChunk = H5P.get_chunk(propId, rank, chunkDims);

                memId = H5S.create_simple(rank, dims, maxDims);
                GCHandle hnd = GCHandle.Alloc(dset, GCHandleType.Pinned);
                H5D.read(datasetId, datatype, memId, spaceId,
                    H5P.DEFAULT, hnd.AddrOfPinnedObject());
                hnd.Free();
            }
            else
                dset = Array.CreateInstance(type, new long[1] { 0 });
            H5D.close(datasetId);
            H5S.close(spaceId);
            return dset;

        }

        /// <summary>
        /// Reads part of a two dimensional dataset.
        /// </summary>
        /// <typeparam name="T">Generic parameter strings or primitive type</typeparam>
        /// <param name="groupId">id of the group. Can also be a file Id</param>
        /// <param name="name">name of the dataset</param>
        /// <param name="beginIndex">The index of the first row to be read</param>
        /// <param name="endIndex">The index of the last row to be read</param>
        /// <returns>The two dimensional dataset</returns>
        public static T[,] ReadDataset<T>(hid_t groupId, string name, ulong beginIndex, ulong endIndex) //where T : struct
        {
            ulong[] start = { 0, 0 }, stride = null, count = { 0, 0 },
                block = null, offsetOut = new ulong[] { 0, 0 };
            var datatype = GetDatatype(typeof(T));

            var datasetId = H5D.open(groupId, name);
            var spaceId = H5D.get_space(datasetId);
            int rank = H5S.get_simple_extent_ndims(spaceId);
            ulong[] maxDims = new ulong[rank];
            ulong[] dims = new ulong[rank];
            ulong[] chunkDims = new ulong[rank];
            var memId_n = H5S.get_simple_extent_dims(spaceId, dims, maxDims);

            start[0] = beginIndex;
            start[1] = 0;
            count[0] = endIndex - beginIndex + 1;
            count[1] = dims[1];

            var status = H5S.select_hyperslab(spaceId, H5S.seloper_t.SET, start, stride, count, block);


            // Define the memory dataspace.
            T[,] dset = new T[count[0], count[1]];
            var memId = H5S.create_simple(rank, count, null);

            // Define memory hyperslab. 
            status = H5S.select_hyperslab(memId, H5S.seloper_t.SET, offsetOut, null,
                         count, null);

            // Read data from hyperslab in the file into the hyperslab in 
            // memory and display.             
            GCHandle hnd = GCHandle.Alloc(dset, GCHandleType.Pinned);
            H5D.read(datasetId, datatype, memId, spaceId,
                H5P.DEFAULT, hnd.AddrOfPinnedObject());
            hnd.Free();
            H5D.close(datasetId);
            H5S.close(spaceId);
            H5S.close(memId);
            return dset;
        }

        /// <summary>
        /// Reads a dataset or string array with one value in it
        /// </summary>
        /// <typeparam name="T">Generic parameter strings or primitive type</typeparam>
        /// <param name="groupId">id of the group. Can also be a file Id</param>
        /// <param name="name">name of the dataset</param>
        /// <returns>One value or string</returns>
        public static T ReadOneValue<T>(hid_t groupId, string name) //where T : struct
        {
            var dset = dsetRW.ReadArray<T>(groupId, name);
            int[] first = new int[dset.Rank].Select(f => 0).ToArray();
            T result = (T)dset.GetValue(first);
            return result;
        }

        public static Array ReadDataset<T>(hid_t groupId, string name)
        {
            return dsetRW.ReadArray<T>(groupId, name);
        }

        /// <summary>
        /// Writes one value to a hdf5 file
        /// </summary>
        /// <typeparam name="T">Generic parameter strings or primitive type</typeparam>
        /// <param name="groupId">id of the group. Can also be a file Id</param>
        /// <param name="name">name of the dataset</param>
        /// <param name="dset">The dataset</param>
        /// <returns>status of the write method</returns>
        public static (int success, hid_t CreatedgroupId) WriteOneValue<T>(hid_t groupId, string name, T dset)
        {
            if (typeof(T) == typeof(string))
                //WriteStrings(groupId, name, new string[] { dset.ToString() });
                return dsetRW.WriteArray(groupId, name, new T[1] { dset });
            else
            {
                Array oneVal = new T[1, 1] { { dset } };
                return dsetRW.WriteArray(groupId, name, oneVal);
            }
        }

        public static void WriteDataset(hid_t groupId, string name, Array collection)
        {
            dsetRW.WriteArray(groupId, name, collection);
        }


        public static (int success, hid_t CreatedgroupId) WriteDatasetFromArray<T>(hid_t groupId, string name, Array dset, string datasetName = null) //where T : struct
        {
            int rank = dset.Rank;
            ulong[] dims = Enumerable.Range(0, rank).Select(i => { return (ulong)dset.GetLength(i); }).ToArray();

            ulong[] maxDims = null;
            var spaceId = H5S.create_simple(rank, dims, maxDims);
            var datatype = GetDatatype(typeof(T));
            var typeId = H5T.copy(datatype);
            if (datatype == H5T.C_S1)
            {
                H5T.set_size(datatype, new IntPtr(2));
            }
            var datasetId = H5D.create(groupId, name, datatype, spaceId);
            GCHandle hnd = GCHandle.Alloc(dset, GCHandleType.Pinned);
            var result = H5D.write(datasetId, datatype, H5S.ALL, H5S.ALL, H5P.DEFAULT,
                hnd.AddrOfPinnedObject());
            hnd.Free();
            H5D.close(datasetId);
            H5S.close(spaceId);
            H5T.close(typeId);
            return (result, datasetId);
        }

        /// <summary>
        /// Appends a dataset to a hdf5 file. If called the first time a dataset is created
        /// </summary>
        /// <typeparam name="T">Generic parameter only primitive types are allowed</typeparam>
        /// <param name="groupId">id of the group. Can also be a file Id</param>
        /// <param name="name">name of the dataset</param>
        /// <param name="dset">The dataset</param>
        /// <returns>status of the write method</returns>
        public static hid_t AppendDataset<T>(hid_t groupId, string name, Array dset, ulong chunkX = 200) where T : struct
        {
            var rank = dset.Rank;
            ulong[] dimsExtend = Enumerable.Range(0, rank).Select(i =>
            { return (ulong)dset.GetLength(i); }).ToArray();
            ulong[] maxDimsExtend = null;
            ulong[] dimsChunk = new ulong[] { chunkX }.Concat(dimsExtend.Skip(1)).ToArray();
            ulong[] zeros = Enumerable.Range(0, rank).Select(z => (ulong)0).ToArray();
            hid_t status, spaceId, datasetId;


            // name = ToHdf5Name(name);
            var datatype = GetDatatype(typeof(T));
            var typeId = H5T.copy(datatype);
            var datasetExists = H5L.exists(groupId, name) > 0;

            /* Create a new dataset within the file using chunk 
               creation properties.  */
            if (!datasetExists)
            {

                spaceId = H5S.create_simple(dset.Rank, dimsExtend, maxDimsExtend);

                var propId = H5P.create(H5P.DATASET_CREATE);
                status = H5P.set_chunk(propId, rank, dimsChunk);
                datasetId = H5D.create(groupId, name, datatype, spaceId,
                                     H5P.DEFAULT, propId, H5P.DEFAULT);
                /* Write data to dataset */
                GCHandle hnd = GCHandle.Alloc(dset, GCHandleType.Pinned);
                status = H5D.write(datasetId, datatype, H5S.ALL, H5S.ALL, H5P.DEFAULT,
                    hnd.AddrOfPinnedObject());
                hnd.Free();
                H5P.close(propId);
            }
            else
            {
                datasetId = H5D.open(groupId, name);
                spaceId = H5D.get_space(datasetId);
                var rank_old = H5S.get_simple_extent_ndims(spaceId);
                ulong[] maxDims = new ulong[rank_old];
                ulong[] dims = new ulong[rank_old];
                var memId1 = H5S.get_simple_extent_dims(spaceId, dims, maxDims);

                ulong[] oldChunk = null;
                int chunkDims = 0;
                var propId = H5P.create(H5P.DATASET_ACCESS);
                status = H5P.get_chunk(propId, chunkDims, oldChunk);

                /* Extend the dataset. */
                var size = new ulong[] { dims[0] + dimsExtend[0] }.Concat(dims.Skip(1)).ToArray();
                status = H5D.set_extent(datasetId, size);

                /* Select a hyperslab in extended portion of dataset  */
                var filespaceId = H5D.get_space(datasetId);
                var offset = new ulong[] { dims[0] }.Concat(zeros.Skip(1)).ToArray();
                status = H5S.select_hyperslab(filespaceId, H5S.seloper_t.SET, offset, null,
                                              dimsExtend, null);

                /* Define memory space */
                var memId2 = H5S.create_simple(rank, dimsExtend, null);

                /* Write the data to the extended portion of dataset  */
                GCHandle hnd = GCHandle.Alloc(dset, GCHandleType.Pinned);
                status = H5D.write(datasetId, datatype, memId2, spaceId,
                                   H5P.DEFAULT, hnd.AddrOfPinnedObject());
                hnd.Free();
                H5S.close(memId1);
                H5S.close(memId2);
                H5D.close(filespaceId);
            }

            H5D.close(datasetId);
            H5S.close(spaceId);
            return status;
        }

    }
}
