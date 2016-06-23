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
    public static partial class Hdf5
    {


        public static IEnumerable<string> ReadStrings(int groupId, string name)
        {

            int datatype = H5T.create(H5T.class_t.STRING, H5T.VARIABLE);
            H5T.set_cset(datatype, H5T.cset_t.UTF8);
            H5T.set_strpad(datatype, H5T.str_t.NULLTERM);

            //name = ToHdf5Name(name);

            var datasetId = H5D.open(groupId, name);
            int spaceId = H5D.get_space(datasetId);

            long count = H5S.get_simple_extent_npoints(spaceId);
            H5S.close(spaceId);

            IntPtr[] rdata = new IntPtr[count];
            GCHandle hnd = GCHandle.Alloc(rdata, GCHandleType.Pinned);
            H5D.read(datasetId, datatype, H5S.ALL, H5S.ALL,
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
            H5T.close(datatype);
            H5D.close(datasetId);
            return strs;
        }


        public static int WriteStrings(int groupId, string name, IEnumerable<string> strs)
        {

            // create UTF-8 encoded test datasets

            int datatype = H5T.create(H5T.class_t.STRING, H5T.VARIABLE);
            H5T.set_cset(datatype, H5T.cset_t.UTF8);
            H5T.set_strpad(datatype, H5T.str_t.SPACEPAD);

            int strSz = strs.Count();
            int spaceId = H5S.create_simple(1,
                new ulong[] { (ulong)strSz }, null);

            //name = ToHdf5Name(name);
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

            H5S.close(spaceId);
            H5T.close(datatype);
            return result;
        }


        //public static void WriteStringsToAscii2(int groupId, string name, IEnumerable<string> strs)
        //{
        //    var maxLength = strs.Select(s => s.Length).Max();
        //    ulong strSz = (ulong)strs.Count();
        //    var buf = strs.SelectMany(s => s.PadRight(maxLength).ToCharArray()).ToString();

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

        //    IntPtr bufArray = Marshal.StringToHGlobalAnsi(buf);
        //    H5A.write(attr, aTypeId, bufArray);
        //    Marshal.FreeHGlobal(bufArray);


        //    /* End access to the dataset and release resources used by it. */
        //    H5D.close(datasetId);

        //    /* Terminate access to the data space. */
        //    H5S.close(spaceId);

        //    H5S.close(aId);
        //    H5T.close(aTypeId);
        //    H5A.close(attr);
        //}
        //public static int WriteStringsToAscii(int groupId, string name, IEnumerable<string> strs)
        //{
        //    var maxLength = strs.Select(s => s.Length).Max();
        //    ulong strSz = (ulong)strs.Count();
        //    var buf = strs.SelectMany(s => s.PadRight(maxLength).ToCharArray()).ToArray();

        //    ulong[] dimsa = new ulong[] { strSz, 1 };

        //    /* Create the data space for the dataset. */
        //    ulong[] dims = new ulong[] { strSz, (ulong)maxLength };
        //    var spaceId = H5S.create_simple(2, dims, null);

        //    /* Create the dataset. */
        //    //name = ToHdf5Name(name);

        //    //var aId = H5S.create_simple(2, dimsa, null);

        //    var aTypeId = H5T.copy(H5T.C_S1);
        //    H5T.set_size(aTypeId, new IntPtr(2));
        //    byte[] wdata = new byte[2 * buf.Length];
        //    for (int i = 0; i < buf.Length; ++i)
        //    {
        //        wdata[2 * i] = (byte)buf[i];
        //    }

        //    var datasetId = H5D.create(groupId, name, H5T.FORTRAN_S1, spaceId);
        //    GCHandle hnd = GCHandle.Alloc(wdata, GCHandleType.Pinned);
        //    var result = H5D.write(datasetId, aTypeId, H5S.ALL, H5S.ALL, H5P.DEFAULT,
        //        hnd.AddrOfPinnedObject());
        //    hnd.Free();
        //    H5D.close(datasetId);
        //    H5S.close(spaceId);
        //    H5T.close(aTypeId);
        //    return result;
        //}


        public static int WriteAsciiString(int groupId, string name, string str)
        {
            var spaceNullId = H5S.create(H5S.class_t.NULL);
            var spaceScalarId = H5S.create(H5S.class_t.SCALAR);

            // create two datasets of the extended ASCII character set
            // store as H5T.FORTRAN_S1 -> space padding

            int strLength = str.Length;
            ulong[] dims = {(ulong)strLength, 1};

            /* Create the dataset. */
            //name = ToHdf5Name(name);

            var spaceId = H5S.create_simple(1, dims, null);
            var datasetId = H5D.create(groupId, name,
                    H5T.FORTRAN_S1, spaceId);
            H5S.close(spaceId);

            // we write from C and must provide null-terminated strings

            byte[] wdata = new byte[strLength*2];
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

        public static string ReadAsciiString(int groupId, string name)
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
                Select(b=>(b==0)?(byte)32:b).ToArray();
            string result = Encoding.ASCII.GetString(wdata);

            H5T.close(memId);
            H5D.close(datasetId);
            return result;
        }

        public static int WriteUnicodeString(int groupId, string name, string str)
        {
            byte[] wdata = Encoding.UTF8.GetBytes(str);

            int spaceId = H5S.create(H5S.class_t.SCALAR);

            hid_t dtype = H5T.create(H5T.class_t.STRING, new IntPtr(wdata.Length));
            H5T.set_cset(dtype, H5T.cset_t.UTF8);
            H5T.set_strpad(dtype, H5T.str_t.SPACEPAD);
            
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

        public static string ReadUnicodeString(int groupId, string name)
        {
            int datatype = H5T.create(H5T.class_t.STRING, H5T.VARIABLE);
            H5T.set_cset(datatype, H5T.cset_t.UTF8);
            H5T.set_strpad(datatype, H5T.str_t.SPACEPAD);

            var datasetId = H5D.open(groupId, name);
            var typeId = H5D.get_type(datasetId);

            var classId = H5T.get_class(typeId);
            var order = H5T.get_order(typeId);
            IntPtr size = H5T.get_size(typeId);
            int strLen = (int)size;

            int spaceId = H5D.get_space(datasetId);

            byte[] wdata = new byte[strLen];

            //IntPtr ptr = new IntPtr();
            GCHandle hnd = GCHandle.Alloc(wdata, GCHandleType.Pinned);
            H5D.read(datasetId, datatype, H5S.ALL, H5S.ALL,
                H5P.DEFAULT, hnd.AddrOfPinnedObject());
            hnd.Free();

            //int len = 0;
            //while (Marshal.ReadByte(ptr, len) != 0) { ++len; }
            //byte[] name_buf = new byte[len];
            //Marshal.Copy(ptr, name_buf, 0, len);
            string s = Encoding.UTF8.GetString(wdata);

            H5S.close(spaceId);
            H5T.close(datatype);
            H5D.close(datasetId);
            return s;
        }
    }

    //public static int Write64Strings(int groupId, string name, string str)
    //{
    //    var spaceNullId = H5S.create(H5S.class_t.NULL);
    //    var spaceScalarId = H5S.create(H5S.class_t.SCALAR);

    //    // store as H5T.FORTRAN_S1 -> space padding

    //    int strSz = 64;
    //    int strLength = 256 / strSz;
    //    ulong[] dims = { (ulong)strSz, 1 };

    //    var wbyte = new byte[256];
    //    for (int i = 0; i < 256; ++i)
    //        wbyte[i] = (byte)i;
    //    var wdata = Encoding.ASCII.GetString(wbyte.ToArray());
    //    wdata.Insert(0,str);

    //    /* Create the dataset. */
    //    //name = ToHdf5Name(name);

    //    var spaceId = H5S.create_simple(1, dims, null);
    //    var datasetId = H5D.create(groupId, name,
    //            H5T.FORTRAN_S1, spaceId);
    //    H5S.close(spaceId);


    //    var memId = H5T.copy(H5T.C_S1);
    //    H5T.set_size(memId, new IntPtr(2));
    //    //H5T.set_strpad(memId, H5T.str_t.NULLTERM);

    //    // we write from C and must provide null-terminated strings
    //    GCHandle[] hnds = new GCHandle[strSz];
    //    IntPtr[] wdata1 = new IntPtr[strSz];
    //    byte[] bytes = new byte[strLength * 2];

    //    for (int i = 0; i < strSz; ++i)
    //    {
    //        for (int j = 0; j < strLength; ++j)
    //        {
    //            var tmp = wdata.Substring(i * strLength, strLength);
    //            bytes[2 * j] = Convert.ToByte(tmp[j]);
    //        }
    //        hnds[i] = GCHandle.Alloc(bytes, GCHandleType.Pinned);
    //        wdata1[i] = hnds[i].AddrOfPinnedObject();
    //    }

    //    var hnd = GCHandle.Alloc(wdata1, GCHandleType.Pinned);

    //    int result = H5D.write(datasetId, memId, H5S.ALL, H5S.ALL,
    //        H5P.DEFAULT, hnd.AddrOfPinnedObject());
    //    hnd.Free();

    //    for (int i = 0; i < strSz; ++i)
    //    {
    //        hnds[i].Free();
    //    }


    //    H5T.close(memId);
    //    H5D.close(datasetId);
    //    return result;
    //}


}
