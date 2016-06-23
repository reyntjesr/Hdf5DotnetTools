using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HDF.PInvoke;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;

namespace Hdf5DotNetTools
{
    public static partial class Hdf5
    {
        // information: https://www.hdfgroup.org/ftp/HDF5/examples/examples-by-api/hdf5-examples/1_8/C/H5T/h5ex_t_cmpd.c

        public static int WriteCompounds<T>(int groupId, string name, IEnumerable<T> array) where T : struct
        {
            int memId=0, filetype=0;
            Type type = typeof(T);
            var fields = type.GetFields();
            //array = changeStrings(array, fields);
            //var compoundSizearraySize = Marshal.SizeOf(array);
            ulong[] dims = new ulong[] { (ulong)array.Count() };

            calcCompoundSize(type,false,ref memId);

            calcCompoundSize(type,true,ref filetype);

            // Create dataspace.  Setting maximum size to NULL sets the maximum
            // size to be the current size.
            var spaceId = H5S.create_simple(1, dims, null);

            // Create the dataset and write the compound data to it.
            var datasetId = H5D.create(groupId, name, filetype, spaceId, H5P.DEFAULT, H5P.DEFAULT, H5P.DEFAULT);

            var ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);
            foreach (var strct in array)
                writer.Write(getBytes(strct));
            var bytes = ms.ToArray();

            GCHandle hnd = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var statusId = H5D.write(datasetId, memId, H5S.ALL, H5S.ALL, H5P.DEFAULT,
                    hnd.AddrOfPinnedObject());
            hnd.Free();
            /*
             * Close and release resources.
             */
            H5D.close(datasetId);
            H5S.close(spaceId);
            H5T.close(filetype);
            return statusId;
        }


        private static IEnumerable<T> changeStrings<T>(IEnumerable<T> array, FieldInfo[] fields) where T : struct
        {
            foreach (var info in fields)
                if (info.FieldType == typeof(string))
                {
                    var attr = info.GetCustomAttributes(typeof(MarshalAsAttribute), false);
                    MarshalAsAttribute maa = (MarshalAsAttribute)attr[0];
                    object value = info.GetValue(array);
                }
            return array;
        }
        //private static int createFiletype(Type type)
        //{
        //    var compoundInfo = Hdf5.GetCompoundInfo(type, true).ToArray();
        //    var curCompound = compoundInfo.Last();
        //    var compoundSize = curCompound.offset + curCompound.size;
        //    var filetype = H5T.create(H5T.class_t.COMPOUND, new IntPtr(compoundSize));
        //    foreach (var cmp in compoundInfo)
        //        H5T.insert(filetype, cmp.name, new IntPtr(cmp.offset), cmp.datatype);
        //    return filetype;
        //}

        private static int calcCompoundSize(Type type,bool useIEEE, ref int id)
        {
            // Create the compound datatype for the file.  Because the standard
            // types we are using for the file may have different sizes than
            // the corresponding native types
            var compoundInfo = Hdf5.GetCompoundInfo(type,useIEEE);
            var curCompound = compoundInfo.Last();
            var compoundSize = curCompound.offset + curCompound.size;
            //Create the compound datatype for memory.
            id = H5T.create(H5T.class_t.COMPOUND, new IntPtr(compoundSize));
            foreach (var cmp in compoundInfo)
                H5T.insert(id, cmp.name, new IntPtr(cmp.offset), cmp.datatype);
            return compoundSize;
        }

        public static IEnumerable<OffsetInfo> GetCompoundInfo(Type type, bool ieee = false)
        {
            //Type t = typeof(T);
            var strtype = H5T.copy(H5T.C_S1);
            int strsize = (int)H5T.get_size(strtype);
            int curSize = 0;
            List<OffsetInfo> offsets = new List<OffsetInfo>();
            foreach (var x in type.GetFields())
            {
                OffsetInfo oi = new OffsetInfo()
                {
                    name = x.Name,
                    type = x.FieldType,
                    datatype = ieee ? GetDatatypeIEEE(x.FieldType) : GetDatatype(x.FieldType),
                    size = x.FieldType == typeof(string) ? stringLength(x) : Marshal.SizeOf(x.FieldType),
                    offset = 0 + curSize
                };
                if (oi.datatype == H5T.C_S1)
                {
                    strtype = H5T.copy(H5T.C_S1);
                    H5T.set_size(strtype, new IntPtr(oi.size));
                    oi.datatype = strtype;
                }
                if (oi.datatype == H5T.STD_I64BE)
                    oi.size = oi.size * 2;
                curSize = curSize + oi.size;

                offsets.Add(oi);
            }
            H5T.close(strtype);
            return offsets;

        }

        private static int stringLength(FieldInfo fld)
        {
            var attr = fld.GetCustomAttributes(typeof(MarshalAsAttribute), false);
            MarshalAsAttribute maa = (MarshalAsAttribute)attr[0];
            var constSize = maa.SizeConst;
            return constSize;
        }

        public static IEnumerable<T> ReadCompounds<T>(int groupId, string name) where T : struct
        {
            Type type = typeof(T);
            int memId = 0;
            // open dataset
            var datasetId = H5D.open(groupId, name);

            var compoundSize = calcCompoundSize(type,false,ref memId);

            /*
             * Get dataspace and allocate memory for read buffer.
             */
            var spaceId = H5D.get_space(datasetId);
            int rank = H5S.get_simple_extent_ndims(spaceId);
            ulong[] dims = new ulong[rank];
            var ndims = H5S.get_simple_extent_dims(spaceId, dims, null);
            int rows = Convert.ToInt32(dims[0]);

            byte[] bytes = new byte[rows*compoundSize];
            // Read the data.
            GCHandle hnd = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            H5D.read(datasetId, memId, H5S.ALL,H5S.ALL, H5P.DEFAULT, hnd.AddrOfPinnedObject());
            hnd.Free();
            int counter = 0;
            IEnumerable<T> strcts = Enumerable.Range(1, rows).Select(i =>
             {
                 byte[] select = new byte[compoundSize];
                 Array.Copy(bytes, counter, select, 0, compoundSize);
                 T s = fromBytes<T>(select);
                 counter = counter + compoundSize;
                 return s;
             });

            return strcts;
        }

    }
}
