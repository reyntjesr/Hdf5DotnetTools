using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HDF.PInvoke;
using System.Runtime.InteropServices;
using System.IO;

namespace Knf.Utils.Hdf5
{
    public static partial class Hdf5
    {

        public static int OpenFile(string filename, bool readOnly = false)
        {
            int fileId;
            if (File.Exists(filename))
            {
                uint access = (readOnly) ? H5F.ACC_RDONLY : H5F.ACC_RDWR;
                fileId = H5F.open(filename, access);
            }
            else
                fileId = H5F.create(filename,
                                             H5F.ACC_TRUNC);//,H5P.DEFAULT,H5P.DEFAULT
            return fileId;
        }

        public static int CloseFile(int fileId)
        {
            return H5F.close(fileId);
        }

        public static T[,] ReadDataset<T>(int groupId, string name) where T : struct
        {
            var datatype = GetDatatype(typeof(T));

            name = ToHdf5Name(name);

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

        public static T[,] ReadDataset<T>(int groupId, string name, ulong beginIndex, ulong endIndex) where T : struct
        {
            ulong[] start = { 0, 0 }, stride = null, count = { 0, 0 },
                block = null, offsetOut = new ulong[] { 0, 0 };
            var datatype = GetDatatype(typeof(T));

            name = ToHdf5Name(name);

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

        public static int WriteDataset<T>(int groupId, string name, T[,] dset) where T : struct
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
            name = ToHdf5Name(name);
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

        public static IEnumerable<string> ReadStrings(int groupId, string name)
        {

            int memId = H5T.create(H5T.class_t.STRING, H5T.VARIABLE);
            H5T.set_cset(memId, H5T.cset_t.UTF8);
            H5T.set_strpad(memId, H5T.str_t.NULLTERM);

            name = ToHdf5Name(name);

            var datasetId = H5D.open(groupId, name);
            int spaceId = H5D.get_space(datasetId);

            long count = H5S.get_simple_extent_npoints(spaceId);
            H5S.close(spaceId);

            IntPtr[] rdata = new IntPtr[count];
            GCHandle hnd = GCHandle.Alloc(rdata, GCHandleType.Pinned);
            H5D.read(datasetId, memId, H5S.ALL, H5S.ALL,
                H5P.DEFAULT, hnd.AddrOfPinnedObject());

            var strs = new List<string>();
            for (int i = 0; i < rdata.Length; ++i)
            {
                int len = 0;
                while (Marshal.ReadByte(rdata[i], len) != 0) { ++len; }
                byte[] buffer = new byte[len];
                Marshal.Copy(rdata[i], buffer, 0, buffer.Length);
                string s = Encoding.UTF8.GetString(buffer);

                strs.Add(s);

                H5.free_memory(rdata[i]);
            }

            hnd.Free();
            H5T.close(memId);
            H5D.close(datasetId);
            return strs;
        }

        public static void WriteStrings(int groupId, string name, IEnumerable<string> strs)
        {

            // create UTF-8 encoded test datasets

            int typeId = H5T.create(H5T.class_t.STRING, H5T.VARIABLE);
            H5T.set_cset(typeId, H5T.cset_t.UTF8);
            H5T.set_strpad(typeId, H5T.str_t.SPACEPAD);

            int strSz = strs.Count();
            int spaceId = H5S.create_simple(1,
                new ulong[] { (ulong)strSz }, null);

            name = ToHdf5Name(name);
            var datasetId = H5D.create(groupId, name, typeId, spaceId);

            GCHandle[] hnds = new GCHandle[strSz];
            IntPtr[] wdata1 = new IntPtr[strSz];

            foreach (var item in strs.Select((str, i) => new { i, str }))
            {
                hnds[item.i] = GCHandle.Alloc(
                    Encoding.UTF8.GetBytes((string)item.str),
                    GCHandleType.Pinned);
                wdata1[item.i] = hnds[item.i].AddrOfPinnedObject();
            }

            var hnd = GCHandle.Alloc(wdata1, GCHandleType.Pinned);

            H5D.write(datasetId, typeId, H5S.ALL, H5S.ALL,
                H5P.DEFAULT, hnd.AddrOfPinnedObject());
            hnd.Free();

            for (int i = 0; i < strSz; ++i)
            {
                hnds[i].Free();
            }

            H5S.close(spaceId);
            H5T.close(typeId);
        }

        //public static void WriteStringsToAscii(int groupId, string name, IEnumerable<string> strs)
        //{
        //    var maxLength = strs.Select(s => s.Length).Max();
        //    ulong strSz = (ulong)strs.Count();
        //    var buf = strs.SelectMany(s => s.PadRight(maxLength).ToCharArray()).ToArray();

        //    ulong[] dimsa = new ulong[] { strSz, 1 };

        //    /* Create the data space for the dataset. */
        //    ulong[] dims = new ulong[] { strSz, (ulong)maxLength + 1 };
        //    var spaceId = H5S.create_simple(2, dims, null);

        //    /* Create the dataset. */
        //    name = ToHdf5Name(name);
        //    var datasetId = H5D.create(groupId, name, H5T.STD_I32BE, spaceId, H5P.DEFAULT);

        //    var aId = H5S.create_simple(2, dimsa, null);

        //    var aTypeId = H5T.copy(H5T.C_S1);
        //    H5T.set_size(aTypeId, new IntPtr(maxLength));

        //    var attr = H5A.create(datasetId, "string-att", aTypeId, aId, H5P.DEFAULT);

        //    GCHandle hnd = GCHandle.Alloc(buf, GCHandleType.Pinned);
        //    H5A.write(attr, aTypeId, hnd.AddrOfPinnedObject());
        //    hnd.Free();

        //    /* End access to the dataset and release resources used by it. */
        //    H5D.close(datasetId);

        //    /* Terminate access to the data space. */
        //    H5S.close(spaceId);

        //    H5S.close(aId);
        //    H5T.close(aTypeId);
        //    H5A.close(attr);
        //}
        public static int WriteStringsToAscii(int groupId, string name, IEnumerable<string> strs)
        {
            var maxLength = strs.Select(s => s.Length).Max();
            ulong strSz = (ulong)strs.Count();
            var buf = strs.SelectMany(s => s.PadRight(maxLength).ToCharArray()).ToArray();

            ulong[] dimsa = new ulong[] { strSz, 1 };

            /* Create the data space for the dataset. */
            ulong[] dims = new ulong[] { strSz, (ulong)maxLength};
            var spaceId = H5S.create_simple(2, dims, null);

            /* Create the dataset. */
            name = ToHdf5Name(name);

            //var aId = H5S.create_simple(2, dimsa, null);

            var aTypeId = H5T.copy(H5T.C_S1);
            H5T.set_size(aTypeId, new IntPtr(2));
            byte[] wdata = new byte[2*buf.Length];
            for (int i = 0; i < buf.Length; ++i)
            {
                wdata[2 * i] = (byte)buf[i];
            }

            var datasetId = H5D.create(groupId, name, H5T.FORTRAN_S1, spaceId);
            GCHandle hnd = GCHandle.Alloc(wdata, GCHandleType.Pinned);
            var result = H5D.write(datasetId, aTypeId, H5S.ALL, H5S.ALL, H5P.DEFAULT,
                hnd.AddrOfPinnedObject());
            hnd.Free();
            H5D.close(datasetId);
            H5S.close(spaceId);
            H5T.close(aTypeId);
            return result;
        }

        public static int AppendDataset<T>(int groupId, string name, T[,] dset, ulong chunkX = 200) where T : struct
        {
            var rank = dset.Rank;
            ulong[] dimsExtend = new ulong[] { (ulong)dset.GetLength(0), (ulong)dset.GetLength(1) };
            ulong[] maxDimsExtend = null;
            ulong[] dimsChunk = new ulong[] { chunkX, (ulong)dset.GetLength(1) };
            int status, spaceId, datasetId;


            name = ToHdf5Name(name);
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
            else {
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


        internal static string ToHdf5Name(string name)
        {
            return string.Concat(@"/", name);
        }

        internal static int GetDatatype(System.Type type)
        {
            var typeName = type.Name;
            int dataType;
            switch (typeName)
            {
                case nameof(Int16):
                    dataType = H5T.NATIVE_INT16;
                    break;
                case nameof(Int32):
                    dataType = H5T.NATIVE_INT;
                    break;
                case nameof(Int64):
                    dataType = H5T.NATIVE_INT64;
                    break;
                case nameof(UInt16):
                    dataType = H5T.NATIVE_UINT16;
                    break;
                case nameof(UInt32):
                    dataType = H5T.NATIVE_UINT32;
                    break;
                case nameof(UInt64):
                    dataType = H5T.NATIVE_UINT64;
                    break;
                case nameof(Double):
                    dataType = H5T.NATIVE_DOUBLE;
                    break;
                case nameof(Boolean):
                    dataType = H5T.NATIVE_INT8;
                    break;
                case nameof(Char):
                    //dataType = H5T.NATIVE_UCHAR;
                    dataType = H5T.C_S1;
                    break;
                default:
                    throw new Exception(string.Format("Datatype {0} not supported", type));
            }
            return dataType;
        }

        //private static T[] getdataOfType<T>(int datatype) where T : struct
        //{
        //    System.Type type;
        //    switch (datatype)
        //    {
        //        case H5T.NATIVE_INT16:
        //            type = Int16.;
        //            break;
        //        case nameof(Int32):
        //            dataType = H5T.NATIVE_INT;
        //            break;
        //        case nameof(Int64):
        //            dataType = H5T.NATIVE_INT64;
        //            break;
        //        case nameof(Double):
        //            dataType = H5T.NATIVE_DOUBLE;
        //            break;
        //        case nameof(Boolean):
        //            dataType = H5T.NATIVE_INT8;
        //            break;
        //        default:
        //            throw new Exception(string.Format("Datatype {0} not supported", type));
        //    }
        //    return type;
        //}
    }
}
