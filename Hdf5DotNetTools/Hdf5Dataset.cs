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
    public static partial class Hdf5
    {

        public static T[,] ReadDataset<T>(int groupId, string name) //where T : struct
        {
            var datatype = GetDatatype(typeof(T));

            //name = ToHdf5Name(name);

            var datasetId = H5D.open(groupId, name);
            var spaceId = H5D.get_space(datasetId);
            int rank = H5S.get_simple_extent_ndims(spaceId);
            long count = H5S.get_simple_extent_npoints(spaceId);
            int rankChunk;
            ulong[] maxDims = new ulong[rank];
            ulong[] dims = new ulong[rank];
            ulong[] chunkDims = new ulong[rank];
            var memId = H5S.get_simple_extent_dims(spaceId, dims, maxDims);
            T[,] dset = new T[dims[0], dims[1]];
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
            H5D.close(typeId);
            H5D.close(datasetId);
            H5S.close(spaceId);
            return dset;

        }

        public static T[,] ReadDataset<T>(int groupId, string name, ulong beginIndex, ulong endIndex) //where T : struct
        {
            ulong[] start = { 0, 0 }, stride = null, count = { 0, 0 },
                block = null, offsetOut = new ulong[] { 0, 0 };
            var datatype = GetDatatype(typeof(T));

            //name = ToHdf5Name(name);

            var datasetId = H5D.open(groupId, name);
            var spaceId = H5D.get_space(datasetId);
            int rank = H5S.get_simple_extent_ndims(spaceId);
            ulong[] maxDims = new ulong[rank];
            ulong[] dims = new ulong[rank];
            ulong[] chunkDims = new ulong[rank];
            var memId_n = H5S.get_simple_extent_dims(spaceId, dims, maxDims);

            start[0] = beginIndex;
            start[1] = 0;
            count[0] = endIndex - beginIndex;
            count[1] = dims[1];

            var status = H5S.select_hyperslab(spaceId, H5S.seloper_t.SET, start, stride, count, block);


            // Define the memory dataspace.

            T[,] dset = new T[count[0], count[1]];
            var memId = H5S.create_simple(rank, count, null);

            // Define memory hyperslab. 
            status = H5S.select_hyperslab(memId, H5S.seloper_t.SET, offsetOut, null,
                         count, null);

            /*
             * Read data from hyperslab in the file into the hyperslab in 
             * memory and display.
             */
            GCHandle hnd = GCHandle.Alloc(dset, GCHandleType.Pinned);
            H5D.read(datasetId, datatype, memId, spaceId,
                H5P.DEFAULT, hnd.AddrOfPinnedObject());
            hnd.Free();
            H5D.close(datasetId);
            H5S.close(spaceId);
            H5S.close(memId);
            return dset;

        }

        public static T ReadPrimitive<T>(int groupId, string name) //where T : struct
        {
            T result;
            if (typeof(T) == typeof(string))
            {
                var strs = ReadStrings(groupId, name);
                result = (T)Convert.ChangeType(strs.First(), typeof(T));
            }
            else
            {
                T[,] temp = ReadDataset<T>(groupId, name);
                result = temp[0, 0];
            }
            return result;
        }

        public static int WritePrimitive<T>(int groupId, string name, T dset)
        {
            int result;
            if (typeof(T) == typeof(string))
                result = WriteStrings(groupId, name, new string[] { dset.ToString() });
            else
                result = WriteDataset(groupId, name, new T[1, 1] { { dset } });
            return result;
        }

        public static int WriteDataset<T>(int groupId, string name, T[,] dset) //where T : struct
        {

            ulong[] dims = new ulong[] { (ulong)dset.GetLength(0), (ulong)dset.GetLength(1) };
            ulong[] maxDims = null;
            var spaceId = H5S.create_simple(2, dims, maxDims);
            var datatype = GetDatatype(typeof(T));
            var typeId = H5T.copy(datatype);
            if (datatype == H5T.C_S1)
            {
                H5T.set_size(datatype, new IntPtr(2));
                //var wdata = Encoding.ASCII.GetBytes((char[,]) dset);
            }
            //name = ToHdf5Name(name);
            var datasetId = H5D.create(groupId, name, datatype, spaceId);
            GCHandle hnd = GCHandle.Alloc(dset, GCHandleType.Pinned);
            var result = H5D.write(datasetId, datatype, H5S.ALL, H5S.ALL, H5P.DEFAULT,
                hnd.AddrOfPinnedObject());
            hnd.Free();
            H5D.close(datasetId);
            H5S.close(spaceId);
            H5T.close(typeId);
            return result;
        }

        public static int AppendDataset<T>(int groupId, string name, T[,] dset, ulong chunkX = 200) where T : struct
        {
            var rank = dset.Rank;
            ulong[] dimsExtend = new ulong[] { (ulong)dset.GetLength(0), (ulong)dset.GetLength(1) };
            ulong[] maxDimsExtend = null;
            ulong[] dimsChunk = new ulong[] { chunkX, (ulong)dset.GetLength(1) };
            int status, spaceId, datasetId;


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

                /* Extend the dataset. Dataset becomes 10 x 3  */
                var size = new ulong[] { dims[0] + dimsExtend[0], dims[1] };
                status = H5D.set_extent(datasetId, size);

                /* Select a hyperslab in extended portion of dataset  */
                var filespaceId = H5D.get_space(datasetId);
                var offset = new ulong[] { dims[0], 0 };
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
