using HDF.PInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hdf5DotNetTools
{
    [System.AttributeUsage(System.AttributeTargets.Class |
                       System.AttributeTargets.Struct)]
    sealed public class Hdf5GroupName : Attribute
    {

        public Hdf5GroupName(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    sealed public class Hdf5Attributes : Attribute
    {

        public Hdf5Attributes(string[] names)
        {
            Names = names;
        }

        public string[] Names { get; private set; }
    }

    sealed public class Hdf5Attribute : Attribute
    {

        public Hdf5Attribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    public static partial class Hdf5
    {
        public static IEnumerable<T> ReadAttributes<T>(int groupId, string name)
        {
            if (typeof(T) == typeof(string))
                return ReadStringAttributes(groupId, name).Cast<T>();
            else
                return ReadPrimitiveAttributes<T>(groupId, name);
        }

        public static T ReadAttribute<T>(int groupId, string name)
        {
            var attrs = ReadAttributes<T>(groupId, name);
            return attrs.First();
        }

        public static IEnumerable<string> ReadStringAttributes(int groupId, string name)
        {

            int datatype = H5T.create(H5T.class_t.STRING, H5T.VARIABLE);
            H5T.set_cset(datatype, H5T.cset_t.UTF8);
            H5T.set_strpad(datatype, H5T.str_t.NULLTERM);

            //name = ToHdf5Name(name);

            var datasetId = H5A.open(groupId, name);
            int spaceId = H5A.get_space(datasetId);

            long count = H5S.get_simple_extent_npoints(spaceId);
            H5S.close(spaceId);

            IntPtr[] rdata = new IntPtr[count];
            GCHandle hnd = GCHandle.Alloc(rdata, GCHandleType.Pinned);
            H5A.read(datasetId, datatype, hnd.AddrOfPinnedObject());

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
            H5A.close(datasetId);
            return strs;
        }

        public static T[] ReadPrimitiveAttributes<T>(int groupId, string name) //where T : struct
        {
            var datatype = GetDatatype(typeof(T));

            var attributeId = H5A.open(groupId, name);
            var spaceId = H5A.get_space(attributeId);
            int rank = H5S.get_simple_extent_ndims(spaceId);
            ulong[] maxDims = new ulong[rank];
            ulong[] dims = new ulong[rank];
            var memId = H5S.get_simple_extent_dims(spaceId, dims, maxDims);
            T[] attributes = new T[dims[0]];

            var typeId = H5A.get_type(attributeId);
            var mem_type = H5T.copy(datatype);
            if (datatype == H5T.C_S1)
                H5T.set_size(datatype, new IntPtr(2));

            var propId = H5A.get_create_plist(attributeId);

            memId = H5S.create_simple(rank, dims, maxDims);
            GCHandle hnd = GCHandle.Alloc(attributes, GCHandleType.Pinned);
            H5A.read(attributeId, datatype, hnd.AddrOfPinnedObject());
            hnd.Free();
            H5A.close(typeId);
            H5A.close(attributeId);
            H5S.close(spaceId);
            return attributes;
        }

        public static int WriteStringAttribute(int groupId, string name, string str, string datasetName = null)
        {
            return WriteStringAttributes(groupId, name, new string[] { str }, datasetName);
        }

        public static int WriteStringAttributes(int groupId, string name, IEnumerable<string> strs,string datasetName = null)
        {
            var tmpId = groupId;
            if (!string.IsNullOrWhiteSpace(datasetName))
            {
                var datasetId = H5D.open(groupId, datasetName);
                if (datasetId > 0)
                    groupId = datasetId;
            }

            // create UTF-8 encoded attributes
            int datatype = H5T.create(H5T.class_t.STRING, H5T.VARIABLE);
            H5T.set_cset(datatype, H5T.cset_t.UTF8);
            H5T.set_strpad(datatype, H5T.str_t.SPACEPAD);

            int strSz = strs.Count();
            int spaceId = H5S.create_simple(1,
                new ulong[] { (ulong)strSz }, null);

            var attributeId = H5A.create(groupId, name, datatype, spaceId);

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

            var result = H5A.write(attributeId, datatype, hnd.AddrOfPinnedObject());
            hnd.Free();

            for (int i = 0; i < strSz; ++i)
            {
                hnds[i].Free();
            }

            H5S.close(spaceId);
            H5T.close(datatype);
            {
                H5D.close(groupId);
            }
            return result;
        }

        public static int WriteAttribute<T>(int groupId, string name, T attribute, string datasetName = null) //where T : struct
        {
            return WriteAttribute<T>(groupId, name, new T[] { attribute }, datasetName);
        }

        public static int WriteAttribute<T>(int groupId, string name, T[] attributes, string datasetName = null) //
        {
            if (attributes.GetType().GetElementType() == typeof(string))
                return WriteStringAttributes(groupId, name, attributes.Cast<string>(), datasetName);
            else
                return WritePrimitiveAttribute(groupId, name, attributes, datasetName);
        }

        public static int WritePrimitiveAttribute<T>(int groupId, string name, T[] attributes, string datasetName = null) //where T : struct
        {
            var tmpId = groupId;
            if (!string.IsNullOrWhiteSpace(datasetName))
            {
                var datasetId = H5D.open(groupId, datasetName);
                if (datasetId > 0)
                    groupId = datasetId;
            }
            ulong[] dim = new ulong[1] { (ulong)attributes.GetLength(0) };
            ulong[] maxDims = null;
            var spaceId = H5S.create_simple(1, dim, maxDims);
            var datatype = GetDatatype(typeof(T));
            var typeId = H5T.copy(datatype);
            var attributeId = H5A.create(groupId, name, datatype, spaceId);
            GCHandle hnd = GCHandle.Alloc(attributes, GCHandleType.Pinned);
            var result = H5A.write(attributeId, datatype, hnd.AddrOfPinnedObject());
            hnd.Free();

            H5A.close(attributeId);
            H5S.close(spaceId);
            H5T.close(typeId);
            if (tmpId != groupId)
            {
                H5D.close(groupId);
            }
            return result;
        }
    }
}

public enum Hdf5Save
{
    Save,
    DoNotSave,
}

[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
sealed public class Hdf5SaveAttribute : System.Attribute
{
    private Hdf5Save saveKind;

    public Hdf5Save SaveKind => saveKind;      // Topic is a named parameter


    public Hdf5SaveAttribute(Hdf5Save saveKind)  // url is a positional parameter
    {
        this.saveKind = saveKind;
    }

}