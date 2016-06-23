using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HDF.PInvoke;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;

#if HDF5_VER1_10
using hid_t = System.Int64;
#else
using hid_t = System.Int32;
#endif


namespace Hdf5DotNetTools
{
    public struct OffsetInfo
    {
        public string name;
        public int offset;
        public int size;
        public Type type;
        public int datatype;
    }


    public static partial class Hdf5
    {
        private static readonly IEnumerable<TypeCode> primitiveTypes = Enumerable.Except(Enum.GetValues(typeof(TypeCode)).Cast<TypeCode>(),
                new TypeCode[] { TypeCode.Empty, TypeCode.DBNull, TypeCode.Object });

        public static int sizeofType<T>(T obj, FieldInfo info)
        {
            Type type = info.FieldType;
            if (type.IsEnum)
            {
                return Marshal.SizeOf(Enum.GetUnderlyingType(type));
            }
            if (type.IsValueType)
            {
                return Marshal.SizeOf(type);
            }
            if (type == typeof(string))
            {
                return Encoding.Default.GetByteCount((char[])info.GetValue(obj));
            }
            return 0;
        }

        public static T fromBytes<T>(byte[] arr) where T : new()
        {
            T strct = new T();

            int size = Marshal.SizeOf(strct);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            strct = (T)Marshal.PtrToStructure(ptr, strct.GetType());
            Marshal.FreeHGlobal(ptr);

            return strct;
        }

        public static byte[] getBytes<T>(T strct)
        {
            int size = Marshal.SizeOf(strct);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(strct, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        /// <summary>
        /// Opens a Hdf-5 file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public static int OpenFile(string filename, bool readOnly = false, bool overwrite = false)
        {
            int fileId;
            uint access = (readOnly) ? H5F.ACC_RDONLY : H5F.ACC_RDWR;
            fileId = H5F.open(filename, access);
            return fileId;
        }

        public static int CreateFile(string filename)
        {
            return H5F.create(filename, H5F.ACC_TRUNC);
        }



        public static int CloseFile(int fileId)
        {
            return H5F.close(fileId);
        }

        //internal static string ToHdf5Name(string name)
        //{
        //    return string.Concat(@"/", name);
        //}


        internal static int GetDatatype(System.Type type)
        {
            //var typeName = type.Name;
            var typeCode = Type.GetTypeCode(type);
            int dataType;
            switch (typeCode)
            {
                case TypeCode.Int16:
                    dataType = H5T.NATIVE_INT16;
                    break;
                case TypeCode.Int32:
                    dataType = H5T.NATIVE_INT32;
                    break;
                case TypeCode.Int64:
                    dataType = H5T.NATIVE_INT64;
                    break;
                case TypeCode.UInt16:
                    dataType = H5T.NATIVE_UINT16;
                    break;
                case TypeCode.UInt32:
                    dataType = H5T.NATIVE_UINT32;
                    break;
                case TypeCode.UInt64:
                    dataType = H5T.NATIVE_UINT64;
                    break;
                case TypeCode.Double:
                    dataType = H5T.NATIVE_DOUBLE;
                    break;
                case TypeCode.Boolean:
                    dataType = H5T.NATIVE_INT8;
                    break;
                case TypeCode.Char:
                    //dataType = H5T.NATIVE_UCHAR;
                    dataType = H5T.C_S1;
                    break;
                case TypeCode.String:
                    //dataType = H5T.NATIVE_UCHAR;
                    dataType = H5T.C_S1;
                    break;
                default:
                    throw new Exception(string.Format("Datatype {0} not supported", type));
            }
            return dataType;
        }

        internal static int GetDatatypeIEEE(System.Type type)
        {
            var typeCode = Type.GetTypeCode(type);
            int dataType;
            switch (typeCode)
            {
                case TypeCode.Int16:
                    dataType = H5T.STD_I32BE;
                    break;
                case TypeCode.Int32:
                    dataType = H5T.STD_I64BE;
                    break;
                case TypeCode.Int64:
                    dataType = H5T.STD_I64BE;
                    break;
                case TypeCode.UInt16:
                    dataType = H5T.STD_U16BE;
                    break;
                case TypeCode.UInt32:
                    dataType = H5T.STD_U64BE;
                    break;
                case TypeCode.UInt64:
                    dataType = H5T.STD_U64BE;
                    break;
                case TypeCode.Double:
                    dataType = H5T.IEEE_F64BE;
                    break;
                case TypeCode.Boolean:
                    dataType = H5T.STD_I8BE;
                    break;
                case TypeCode.Char:
                    //dataType = H5T.NATIVE_UCHAR;
                    dataType = H5T.C_S1;
                    break;
                case TypeCode.String:
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
