using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HDF.PInvoke;
using System.Runtime.InteropServices;
using System.IO;
using hsize_t = System.UInt64;

#if HDF5_VER1_10
using hid_t = System.Int64;
#else
using hid_t = System.Int32;
#endif

namespace Hdf5DotNetTools
{
#if HDF5_VER1_10
    using hid_t = System.Int64;
#else
    using hid_t = System.Int32;
#endif
    public static partial class Hdf5
    {


        public static IEnumerable<string> ReadStrings(hid_t groupId, string name)
        {

            hid_t datatype = H5T.create(H5T.class_t.STRING, H5T.VARIABLE);
            H5T.set_cset(datatype, H5T.cset_t.UTF8);
            H5T.set_strpad(datatype, H5T.str_t.NULLTERM);

            //name = ToHdf5Name(name);

            var datasetId = H5D.open(groupId, name);
            hid_t spaceId = H5D.get_space(datasetId);

            long count = H5S.get_simple_extent_npoints(spaceId);
            H5S.close(spaceId);

            var strs = new List<string>();
            if (count >= 0)
            {
                IntPtr[] rdata = new IntPtr[count];
                GCHandle hnd = GCHandle.Alloc(rdata, GCHandleType.Pinned);
                H5D.read(datasetId, datatype, H5S.ALL, H5S.ALL,
                    H5P.DEFAULT, hnd.AddrOfPinnedObject());

                for (int i = 0; i < rdata.Length; ++i)
                {
                    int len = 0;
                    while (Marshal.ReadByte(rdata[i], len) != 0) { ++len; }
                    byte[] buffer = new byte[len];
                    Marshal.Copy(rdata[i], buffer, 0, buffer.Length);
                    string s = Encoding.UTF8.GetString(buffer);

                    strs.Add(s);

                    // H5.free_memory(rdata[i]);
                }
                hnd.Free();
            }
            H5T.close(datatype);
            H5D.close(datasetId);
            return strs;
        }


        public static (int success, hid_t CreatedgroupId) WriteStrings(hid_t groupId, string name, IEnumerable<string> strs, string datasetName = null)
        {

            // create UTF-8 encoded test datasets

            hid_t datatype = H5T.create(H5T.class_t.STRING, H5T.VARIABLE);
            H5T.set_cset(datatype, H5T.cset_t.UTF8);
            H5T.set_strpad(datatype, H5T.str_t.SPACEPAD);

            int strSz = strs.Count();
            hid_t spaceId = H5S.create_simple(1,
                new ulong[] { (ulong)strSz }, null);

            var datasetId = H5D.create(groupId, name, datatype, spaceId);

            GCHandle[] hnds = new GCHandle[strSz];
            IntPtr[] wdata = new IntPtr[strSz];

            int cntr = 0;
            foreach (string str in strs)
            {
                hnds[cntr] = GCHandle.Alloc(
                    Encoding.UTF8.GetBytes(str),
                    GCHandleType.Pinned);
                wdata[cntr] = hnds[cntr].AddrOfPinnedObject();
                cntr++;
            }

            var hnd = GCHandle.Alloc(wdata, GCHandleType.Pinned);

            var result = H5D.write(datasetId, datatype, H5S.ALL, H5S.ALL,
                H5P.DEFAULT, hnd.AddrOfPinnedObject());
            hnd.Free();

            for (int i = 0; i < strSz; ++i)
            {
                hnds[i].Free();
            }

            H5D.close(datasetId);
            H5S.close(spaceId);
            H5T.close(datatype);
            return (result, datasetId);
        }

        public static int WriteAsciiString(hid_t groupId, string name, string str)
        {
            var spaceNullId = H5S.create(H5S.class_t.NULL);
            var spaceScalarId = H5S.create(H5S.class_t.SCALAR);

            // create two datasets of the extended ASCII character set
            // store as H5T.FORTRAN_S1 -> space padding

            int strLength = str.Length;
            ulong[] dims = { (ulong)strLength, 1 };

            /* Create the dataset. */
            //name = ToHdf5Name(name);

            var spaceId = H5S.create_simple(1, dims, null);
            var datasetId = H5D.create(groupId, name,
                    H5T.FORTRAN_S1, spaceId);
            H5S.close(spaceId);

            // we write from C and must provide null-terminated strings

            byte[] wdata = new byte[strLength * 2];
            //for (int i = 0; i < strLength; ++i)
            //{
            //    wdata[2 * i] = (byte)i;
            //}
            for (int i = 0; i < strLength; ++i)
            {
                wdata[2 * i] = Convert.ToByte(str[i]);
            }

            var memId = H5T.copy(H5T.C_S1);
            H5T.set_size(memId, new IntPtr(2));
            //H5T.set_strpad(memId, H5T.str_t.NULLTERM);
            GCHandle hnd = GCHandle.Alloc(wdata, GCHandleType.Pinned);
            int result = H5D.write(datasetId, memId, H5S.ALL,
                        H5S.ALL, H5P.DEFAULT, hnd.AddrOfPinnedObject());
            hnd.Free();
            H5T.close(memId);
            H5D.close(datasetId);
            return result;
        }

        public static string ReadAsciiString(hid_t groupId, string name)
        {
            var datatype = H5T.FORTRAN_S1;

            //name = ToHdf5Name(name);

            var datasetId = H5D.open(groupId, name);
            var spaceId = H5D.get_space(datasetId);
            int rank = H5S.get_simple_extent_ndims(spaceId);
            ulong[] maxDims = new ulong[rank];
            ulong[] dims = new ulong[rank];
            ulong[] chunkDims = new ulong[rank];
            var memId_n = H5S.get_simple_extent_dims(spaceId, dims, null);
            // we write from C and must provide null-terminated strings

            byte[] wdata = new byte[dims[0] * 2];

            var memId = H5T.copy(H5T.C_S1);
            H5T.set_size(memId, new IntPtr(2));
            //H5T.set_strpad(memId, H5T.str_t.NULLTERM);
            GCHandle hnd = GCHandle.Alloc(wdata, GCHandleType.Pinned);
            int resultId = H5D.read(datasetId, memId, H5S.ALL,
                        H5S.ALL, H5P.DEFAULT, hnd.AddrOfPinnedObject());
            hnd.Free();

            wdata = wdata.Where((b, i) => i % 2 == 0).
                Select(b => (b == 0) ? (byte)32 : b).ToArray();
            string result = Encoding.ASCII.GetString(wdata);

            H5T.close(memId);
            H5D.close(datasetId);
            return result;
        }

        public static int WriteUnicodeString(hid_t groupId, string name, string str, H5T.str_t strPad = H5T.str_t.SPACEPAD)
        {
            byte[] wdata = Encoding.UTF8.GetBytes(str);

            hid_t spaceId = H5S.create(H5S.class_t.SCALAR);

            hid_t dtype = H5T.create(H5T.class_t.STRING, new IntPtr(wdata.Length));
            H5T.set_cset(dtype, H5T.cset_t.UTF8);
            H5T.set_strpad(dtype, strPad);

            hid_t datasetId = H5D.create(groupId, name, dtype, spaceId);

            GCHandle hnd = GCHandle.Alloc(wdata, GCHandleType.Pinned);
            int result = H5D.write(datasetId, dtype, H5S.ALL,
                H5S.ALL, H5P.DEFAULT, hnd.AddrOfPinnedObject());
            hnd.Free();

            H5T.close(dtype);
            H5D.close(datasetId);
            H5S.close(spaceId);
            return result;
        }

        public static string ReadUnicodeString(hid_t groupId, string name)
        {
            var datasetId = H5D.open(groupId, name);
            var typeId = H5D.get_type(datasetId);

            if (H5T.is_variable_str(typeId) > 0)
            {
                var spaceId = H5D.get_space(datasetId);
                hid_t count = H5S.get_simple_extent_npoints(spaceId);

                IntPtr[] rdata = new IntPtr[count];

                GCHandle hnd = GCHandle.Alloc(rdata, GCHandleType.Pinned);
                H5D.read(datasetId, typeId, H5S.ALL, H5S.ALL,
                    H5P.DEFAULT, hnd.AddrOfPinnedObject());

                var attrStrings = new List<string>();
                for (int i = 0; i < rdata.Length; ++i)
                {
                    int attrLength = 0;
                    while (Marshal.ReadByte(rdata[i], attrLength) != 0)
                    {
                        ++attrLength;
                    }

                    byte[] buffer = new byte[attrLength];
                    Marshal.Copy(rdata[i], buffer, 0, buffer.Length);

                    string stringPart = Encoding.UTF8.GetString(buffer);

                    attrStrings.Add(stringPart);

                    H5.free_memory(rdata[i]);
                }

                hnd.Free();
                H5S.close(spaceId);
                H5D.close(datasetId);

                return attrStrings[0];
            }

            // Must be a non-variable length string.
            int size = H5T.get_size(typeId).ToInt32();
            IntPtr iPtr = Marshal.AllocHGlobal(size);

            int result = H5D.read(datasetId, typeId, H5S.ALL, H5S.ALL,
                H5P.DEFAULT, iPtr);
            if (result < 0)
            {
                throw new IOException("Failed to read dataset");
            }

            var strDest = new byte[size];
            Marshal.Copy(iPtr, strDest, 0, size);
            Marshal.FreeHGlobal(iPtr);

            H5D.close(datasetId);

            return Encoding.UTF8.GetString(strDest).TrimEnd((Char)0);
        }
    }

}
