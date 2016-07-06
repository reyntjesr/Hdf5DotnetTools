using HDF.PInvoke;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hdf5DotNetTools
{
    public partial class Hdf5
    {

        /*public static void WriteValue(int groupId, string name, object prim)
        {
            Type type = prim.GetType();

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    Hdf5.WriteTmpArray(groupId, name, new Boolean[,] { { (bool)prim } });
                    break;

                case TypeCode.Byte:
                    Hdf5.WriteTmpArray(groupId, name, new Byte[,] { { (Byte)prim } });
                    break;

                case TypeCode.Char:
                    Hdf5.WriteTmpArray(groupId, name, new Char[,] { { (Char)prim } });
                    break;

                case TypeCode.DateTime:
                    Hdf5.WriteTmpArray(groupId, name, new DateTime[,] { { (DateTime)prim } });
                    break;

                case TypeCode.Decimal:
                    Hdf5.WriteTmpArray(groupId, name, new Decimal[,] { { (Decimal)prim } });
                    break;

                case TypeCode.Double:
                    Hdf5.WriteTmpArray(groupId, name, new Double[,] { { (Double)prim } });
                    break;

                case TypeCode.Int16:
                    Hdf5.WriteTmpArray(groupId, name, new Int16[1] {  (Int16)prim } );
                    break;

                case TypeCode.Int32:
                    Hdf5.WriteTmpArray(groupId, name, new Int32[,] { { (Int32)prim } });
                    break;

                case TypeCode.Int64:
                    Hdf5.WriteTmpArray(groupId, name, new Int64[,] { { (Int64)prim } });
                    break;

                case TypeCode.SByte:
                    Hdf5.WriteTmpArray(groupId, name, new SByte[,] { { (SByte)prim } });
                    break;

                case TypeCode.Single:
                    Hdf5.WriteTmpArray(groupId, name, new Single[,] { { (Single)prim } });
                    break;

                case TypeCode.UInt16:
                    Hdf5.WriteTmpArray(groupId, name, new UInt16[,] { { (UInt16)prim } });
                    break;

                case TypeCode.UInt32:
                    Hdf5.WriteTmpArray(groupId, name, new UInt32[,] { { (UInt32)prim } });
                    break;

                case TypeCode.UInt64:
                    Hdf5.WriteTmpArray(groupId, name, new UInt64[,] { { (UInt64)prim } });
                    break;

                case TypeCode.String:
                    Hdf5.WriteStrings(groupId, name, new string[]  { (string)prim  });
                    break;

                default:
                    if (type == typeof(TimeSpan))
                        Hdf5.WriteTmpArray(groupId, name, new TimeSpan[,] { { (TimeSpan)prim } });
                    else { 
                        string str = "type is not supported: ";
                        throw new NotSupportedException(str + type.FullName);
                    }
                    break;
            }
           // WriteHdf5Attributes(type, groupId, name, name);
        }*/

        private static T[,] convertArrayToType<T>(Array collection)
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
        }

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
            for (var index = firstIndex(array); index != null; index = nextIndex(array, index))
            {
                var v = (T1)array.GetValue(index);
                ar.SetValue(convertFunc(v), index);
            }

            return ar;
        }
        public static void WriteArray(int groupId, string name, Array collection)
        {
            dsetRW.WriteArray(groupId, name, collection);
        }

        public static Array ReadArray<T>(int groupId, string name)
        {
            return dsetRW.ReadArray<T>(groupId, name);
        }
        /*public static void WriteTmpArray(int groupId, string name, Array collection)
        {

            Type type = collection.GetType();
            Type elementType = type.GetElementType();
            TypeCode typeCode = Type.GetTypeCode(elementType);

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    Hdf5.WriteDatasetFromArray<UInt16>(groupId, name, collection.ConvertArray<Boolean, UInt16>(bl => Convert.ToUInt16(bl)));
                    Hdf5.WriteAttribute<string>(groupId, name, "Boolean", name);
                    break;

                case TypeCode.Byte:
                    Hdf5.WriteDatasetFromArray<Byte>(groupId, name, collection);
                    Hdf5.WriteAttribute<string>(groupId, name, "Byte", name);
                    break;

                case TypeCode.Char:
                    Hdf5.WriteDatasetFromArray<UInt16>(groupId, name, collection.ConvertArray<Char, UInt16>(c => Convert.ToUInt16(c)));
                    Hdf5.WriteAttribute<string>(groupId, name, "Char", name);
                    break;

                case TypeCode.DateTime:
                    var dts = collection.ConvertArray<DateTime, long>(dt => dt.Ticks);
                    Hdf5.WriteDatasetFromArray<long>(groupId, name, dts);
                    Hdf5.WriteAttribute<string>(groupId, name, "DateTime", name);
                    break;

                case TypeCode.Decimal:
                    var decs = collection.ConvertArray<decimal, double>(dc => Convert.ToDouble(dc));
                    Hdf5.WriteDatasetFromArray<double>(groupId, name, decs);
                    Hdf5.WriteAttribute<string>(groupId, name, "Decimal", name);
                    break;

                case TypeCode.Double:
                    Hdf5.WriteDatasetFromArray<double>(groupId, name, collection);
                    break;

                case TypeCode.Int16:
                    Hdf5.WriteDatasetFromArray<short>(groupId, name, collection);
                    break;

                case TypeCode.Int32:
                    Hdf5.WriteDatasetFromArray<Int32>(groupId, name, collection);
                    break;

                case TypeCode.Int64:
                    Hdf5.WriteDatasetFromArray<Int64>(groupId, name, collection);
                    break;

                case TypeCode.SByte:
                    Hdf5.WriteDatasetFromArray<SByte>(groupId, name, collection);
                    Hdf5.WriteAttribute<string>(groupId, name, "SByte", name);
                    break;

                case TypeCode.Single:
                    Hdf5.WriteDatasetFromArray<Single>(groupId, name, collection);
                    break;

                case TypeCode.UInt16:
                    Hdf5.WriteDatasetFromArray<UInt16>(groupId, name, collection);
                    break;

                case TypeCode.UInt32:
                    Hdf5.WriteDatasetFromArray<UInt32>(groupId, name, collection);
                    break;

                case TypeCode.UInt64:
                    Hdf5.WriteDatasetFromArray<UInt64>(groupId, name, collection);
                    break;

                case TypeCode.String:
                    if (collection.Rank > 1 && collection.GetLength(1) > 1)
                        throw new Exception("Only 1 dimensional string arrays allowed: " + name);
                    Hdf5.WriteStrings(groupId, name, (string[])collection);
                    break;

                default:
                    if (elementType == typeof(TimeSpan))
                    {
                        var tss = collection.ConvertArray<TimeSpan, long>(dt => dt.Ticks);
                        Hdf5.WriteDatasetFromArray<Int64>(groupId, name, tss);
                        Hdf5.WriteAttribute<string>(groupId, name, "TimeSpan", name);

                    }
                    else
                    {
                        string str = "type is not supported: ";
                        throw new NotSupportedException(str + elementType.FullName);
                    }
                    break;
            }
            //WriteHdf5Attributes(type, groupId, name, name);
        }*/


        /*public static Array ReadTmpArray<T>(int groupId, string name)
        {
            return ReadTmpArray(typeof(T), groupId, name);
        }

        public static Array ReadTmpArray(Type elementType, int groupId, string name)
        {
            TypeCode ty = Type.GetTypeCode(elementType);

            switch (ty)
            {
                case TypeCode.Boolean:
                    var bls = Hdf5.ReadTmpDataset<UInt16>(groupId, name);
                    return bls.ConvertArray<UInt16, bool>(c => Convert.ToBoolean(c)); ;

                case TypeCode.Byte:
                    return Hdf5.ReadTmpDataset<byte>(groupId, name);

                case TypeCode.Char:
                    var chrs = Hdf5.ReadTmpDataset<UInt16>(groupId, name);
                    return chrs.ConvertArray<UInt16, char>(c => Convert.ToChar(c)); ;

                case TypeCode.DateTime:
                    var ticks = Hdf5.ReadTmpDataset<long>(groupId, name);
                    return ticks.ConvertArray<long, DateTime>(tc => new DateTime(tc));

                case TypeCode.Decimal:
                    var decs = Hdf5.ReadTmpDataset<double>(groupId, name);
                    return decs.ConvertArray<double, Decimal>(tc => Convert.ToDecimal(tc));

                case TypeCode.Double:
                    return Hdf5.ReadTmpDataset<double>(groupId, name);

                case TypeCode.Int16:
                    return Hdf5.ReadTmpDataset<Int16>(groupId, name);

                case TypeCode.Int32:
                    return Hdf5.ReadTmpDataset<Int32>(groupId, name);

                case TypeCode.Int64:
                    return Hdf5.ReadTmpDataset<Int64>(groupId, name);

                case TypeCode.SByte:
                    return Hdf5.ReadTmpDataset<SByte>(groupId, name);

                case TypeCode.Single:
                    return Hdf5.ReadTmpDataset<Single>(groupId, name);

                case TypeCode.UInt16:
                    return Hdf5.ReadTmpDataset<UInt16>(groupId, name);

                case TypeCode.UInt32:
                    return Hdf5.ReadTmpDataset<UInt32>(groupId, name);

                case TypeCode.UInt64:
                    return Hdf5.ReadTmpDataset<UInt64>(groupId, name);

                case TypeCode.String:
                    return Hdf5.ReadStrings(groupId, name).ToArray();

                default:
                    if (elementType == typeof(TimeSpan))
                    {
                        var tss = Hdf5.ReadTmpDataset<long>(groupId, name);
                        return tss.ConvertArray<long, TimeSpan>(tcks => new TimeSpan(tcks));
                    }
                    string str = "type is not supported: ";
                    throw new NotSupportedException(str + elementType.FullName);

            }
        }*/

    }
}