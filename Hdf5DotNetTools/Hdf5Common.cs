using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HDF.PInvoke;
using System.Runtime.InteropServices;
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
        public hid_t datatype;
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
        public static hid_t OpenFile(string filename, bool readOnly = false)
        {
            uint access = (readOnly) ? H5F.ACC_RDONLY : H5F.ACC_RDWR;
            var fileId = H5F.open(filename, access);
            return fileId;
        }

        public static hid_t CreateFile(string filename)
        {
            return H5F.create(filename, H5F.ACC_TRUNC);
        }



        public static hid_t CloseFile(hid_t fileId)
        {
            return H5F.close(fileId);
        }

        //internal static string ToHdf5Name(string name)
        //{
        //    return string.Concat(@"/", name);
        //}


        internal static hid_t GetDatatype(System.Type type)
        {
            //var typeName = type.Name;
            hid_t dataType;

            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Byte:
                    dataType = H5T.NATIVE_INT8;
                    break;
                case TypeCode.SByte:
                    dataType = H5T.NATIVE_UINT8;
                    break;
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
                case TypeCode.Single:
                    dataType = H5T.NATIVE_FLOAT;
                    break;
                case TypeCode.Double:
                    dataType = H5T.NATIVE_DOUBLE;
                    break;
                //case TypeCode.DateTime:
                //    dataType = H5T.Native_t;
                //    break;
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

        internal static hid_t GetDatatypeIEEE(System.Type type)
        {
            var typeCode = Type.GetTypeCode(type);
            hid_t dataType;
            switch (typeCode)
            {
                case TypeCode.Int16:
                    dataType = H5T.STD_I16BE;
                    break;
                case TypeCode.Int32:
                    dataType = H5T.STD_I32BE;
                    break;
                case TypeCode.Int64:
                    dataType = H5T.STD_I64BE;
                    break;
                case TypeCode.UInt16:
                    dataType = H5T.STD_U16BE;
                    break;
                case TypeCode.UInt32:
                    dataType = H5T.STD_U32BE;
                    break;
                case TypeCode.UInt64:
                    dataType = H5T.STD_U64BE;
                    break;
                case TypeCode.Single:
                    dataType = H5T.IEEE_F32BE;
                    break;
                case TypeCode.Double:
                    dataType = H5T.IEEE_F64BE;
                    break;
                case TypeCode.Boolean:
                    dataType = H5T.STD_I8BE;
                    break;
                case TypeCode.Char:
                    //dataType = H5T.NATIVE_UCHAR;
                    dataType = H5T.STD_I8BE;
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

        /*private static T[,] convertArrayToType<T>(Array collection)
        {
            System.Collections.IEnumerator myEnumerator = collection.GetEnumerator();
            if (collection.Rank > 2)
                throw new Exception("rank of the array rank is to high");
            int rows = collection.GetLength(0);
            int cols = (collection.Rank == 1) ? 1 : collection.GetLength(1);
            T[,] output = new T[rows, cols];
            for (int row = 0; row <= collection.GetUpperBound(0); row++)
                if (cols == 1)
                    output[row, 0] = (T)collection.GetValue(row);
                else
                    for (int col = 0; col <= collection.GetUpperBound(1); col++)
                        output[row, col] = (T)collection.GetValue(row, col);
            return output;
        }*/

        /// <summary>
        /// http://stackoverflow.com/questions/9914230/iterate-through-an-array-of-arbitrary-dimension
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static Array ConvertArray<T1, T2>(this Array array, Func<T1, T2> convertFunc)
        {
            // Gets the lengths and lower bounds of the input array
            int[] lowerBounds = new int[array.Rank];
            int[] lengths = new int[array.Rank];
            for (int numDimension = 0; numDimension < array.Rank; numDimension++)
            {
                lowerBounds[numDimension] = array.GetLowerBound(numDimension);
                lengths[numDimension] = array.GetLength(numDimension);
            }
            Func<Array, int[]> firstIndex = a => Enumerable.Range(0, a.Rank).Select(_i => a.GetLowerBound(_i)).ToArray();

            Func<Array, int[], int[]> nextIndex = (a, index) =>
            {
                for (int i = index.Length - 1; i >= 0; --i)
                {
                    index[i]++;
                    if (index[i] <= array.GetUpperBound(i))
                        return index;
                    index[i] = array.GetLowerBound(i);
                }
                return null;
            };

            Type type = typeof(T2);
            Array ar = Array.CreateInstance(type, lengths, lowerBounds);
            if (lowerBounds[0] != 0 || lengths[0] != 0)
                for (var index = firstIndex(array); index != null; index = nextIndex(array, index))
                {
                    var v = (T1)array.GetValue(index);
                    ar.SetValue(convertFunc(v), index);
                }

            return ar;
        }

        private static T[] convert2DtoArray<T>(T[,] set)
        {
            int rows = set.GetLength(0);
            int cols = set.GetLength(1);
            T[] output = new T[cols * rows];
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    output[i * cols + j] = set[j, i];
                }
            }
            return output;
        }

        private static Tout[] convert2DtoArray<Tin, Tout>(Tin[,] set, Func<Tin, Tout> convert)
        {
            int rows = set.GetLength(0);
            int cols = set.GetLength(1);
            Tout[] output = new Tout[cols * rows];
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    output[i * cols + j] = convert(set[j, i]);
                }
            }
            return output;
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

        public static bool Similar(this IEnumerable<double> first, IEnumerable<double> second, double precision = 1e-2)
        {
            var result = first.Zip(second, (f, s) => Math.Abs(f - s) < precision);
            return result.All(r => r);
        }

    }
}
