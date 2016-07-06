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
    public class Hdf5Dataset : IHdf5ReaderWriter
    {
        public Array ReadToArray<T>(int groupId, string name)
        {
            return Hdf5.ReadDatasetToArray<T>(groupId, name);
        }

        public void WriteFromArray<T>(int groupId, string name, Array dset, string datasetName = null)
        {
            Hdf5.WriteDatasetFromArray<T>(groupId, name, dset, datasetName);
        }
        public void WriteStrings(int groupId, string name, IEnumerable<string> collection, string datasetName = null)
        {
            Hdf5.WriteStrings(groupId, name, (string[])collection, datasetName);
        }
        public IEnumerable<string> ReadStrings(int groupId, string name)
        {
            return Hdf5.ReadStrings(groupId, name);
        }

    }

    public class Hdf5AttributeRW : IHdf5ReaderWriter
    {
        public Array ReadToArray<T>(int groupId, string name)
        {
            return Hdf5.ReadPrimitiveAttributes<T>(groupId, name);
        }

        public void WriteFromArray<T>(int groupId, string name, Array dset, string datasetName = null)
        {
            Hdf5.WritePrimitiveAttribute<T>(groupId, name, dset, datasetName);
        }

        public void WriteStrings(int groupId, string name, IEnumerable<string> collection, string datasetName = null)
        {
            Hdf5.WriteStringAttributes(groupId, name, (string[])collection, datasetName);
        }

        public IEnumerable<string> ReadStrings(int groupId, string name)
        {
            return Hdf5.ReadStringAttributes(groupId, name);
        }

    }

    public interface IHdf5ReaderWriter
    {
        void WriteFromArray<T>(int groupId, string name, Array dset, string datasetName = null);
        Array ReadToArray<T>(int groupId, string name);

        void WriteStrings(int groupId, string name, IEnumerable<string> collection, string datasetName = null);
        IEnumerable<string> ReadStrings(int groupId, string name);

    }

    public class Hdf5ReaderWriter
    {
        IHdf5ReaderWriter rw;
        public Hdf5ReaderWriter(IHdf5ReaderWriter _rw)
        {
            rw = _rw;
        }

        public void WriteArray(int groupId, string name, Array collection, string datasetName = null)
        {

            Type type = collection.GetType();
            Type elementType = type.GetElementType();
            TypeCode typeCode = Type.GetTypeCode(elementType);

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    var bls = collection.ConvertArray<Boolean, UInt16>(bl => Convert.ToUInt16(bl));
                    rw.WriteFromArray<UInt16>(groupId, name, bls);
                    Hdf5.WriteAttribute<string>(groupId, name, "Boolean", name);
                    break;

                case TypeCode.Byte:
                    rw.WriteFromArray<Byte>(groupId, name, collection);
                    Hdf5.WriteAttribute<string>(groupId, name, "Byte", name);
                    break;

                case TypeCode.Char:
                    rw.WriteFromArray<UInt16>(groupId, name, collection.ConvertArray<Char, UInt16>(c => Convert.ToUInt16(c)));
                    Hdf5.WriteAttribute<string>(groupId, name, "Char", name);
                    break;

                case TypeCode.DateTime:
                    var dts = collection.ConvertArray<DateTime, long>(dt => dt.Ticks);
                    rw.WriteFromArray<long>(groupId, name, dts);
                    Hdf5.WriteAttribute<string>(groupId, name, "DateTime", name);
                    break;

                case TypeCode.Decimal:
                    var decs = collection.ConvertArray<decimal, double>(dc => Convert.ToDouble(dc));
                    rw.WriteFromArray<double>(groupId, name, decs);
                    Hdf5.WriteAttribute<string>(groupId, name, "Decimal", name);
                    break;

                case TypeCode.Double:
                    rw.WriteFromArray<double>(groupId, name, collection);
                    break;

                case TypeCode.Int16:
                    rw.WriteFromArray<short>(groupId, name, collection);
                    break;

                case TypeCode.Int32:
                    rw.WriteFromArray<Int32>(groupId, name, collection);
                    break;

                case TypeCode.Int64:
                    rw.WriteFromArray<Int64>(groupId, name, collection);
                    break;

                case TypeCode.SByte:
                    rw.WriteFromArray<SByte>(groupId, name, collection);
                    Hdf5.WriteAttribute<string>(groupId, name, "SByte", name);
                    break;

                case TypeCode.Single:
                    rw.WriteFromArray<Single>(groupId, name, collection);
                    break;

                case TypeCode.UInt16:
                    rw.WriteFromArray<UInt16>(groupId, name, collection);
                    break;

                case TypeCode.UInt32:
                    rw.WriteFromArray<UInt32>(groupId, name, collection);
                    break;

                case TypeCode.UInt64:
                    rw.WriteFromArray<UInt64>(groupId, name, collection);
                    break;

                case TypeCode.String:
                    if (collection.Rank > 1 && collection.GetLength(1) > 1)
                        throw new Exception("Only 1 dimensional string arrays allowed: " + name);
                    rw.WriteStrings(groupId, name, (string[])collection);
                    break;

                default:
                    if (elementType == typeof(TimeSpan))
                    {
                        var tss = collection.ConvertArray<TimeSpan, long>(dt => dt.Ticks);
                        rw.WriteFromArray<Int64>(groupId, name, tss);
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
        }


        public Array ReadArray<T>(int groupId, string name)
        {
            return ReadArray(typeof(T), groupId, name);
        }

        public Array ReadArray(Type elementType, int groupId, string name)
        {
            TypeCode ty = Type.GetTypeCode(elementType);

            switch (ty)
            {
                case TypeCode.Boolean:
                    var bls = rw.ReadToArray<UInt16>(groupId, name);
                    return bls.ConvertArray<UInt16, bool>(c => Convert.ToBoolean(c)); ;

                case TypeCode.Byte:
                    return rw.ReadToArray<byte>(groupId, name);

                case TypeCode.Char:
                    var chrs = rw.ReadToArray<UInt16>(groupId, name);
                    return chrs.ConvertArray<UInt16, char>(c => Convert.ToChar(c)); ;

                case TypeCode.DateTime:
                    var ticks = rw.ReadToArray<long>(groupId, name);
                    return ticks.ConvertArray<long, DateTime>(tc => new DateTime(tc));

                case TypeCode.Decimal:
                    var decs = rw.ReadToArray<double>(groupId, name);
                    return decs.ConvertArray<double, Decimal>(tc => Convert.ToDecimal(tc));

                case TypeCode.Double:
                    return rw.ReadToArray<double>(groupId, name);

                case TypeCode.Int16:
                    return rw.ReadToArray<Int16>(groupId, name);

                case TypeCode.Int32:
                    return rw.ReadToArray<Int32>(groupId, name);

                case TypeCode.Int64:
                    return rw.ReadToArray<Int64>(groupId, name);

                case TypeCode.SByte:
                    return rw.ReadToArray<SByte>(groupId, name);

                case TypeCode.Single:
                    return rw.ReadToArray<Single>(groupId, name);

                case TypeCode.UInt16:
                    return rw.ReadToArray<UInt16>(groupId, name);

                case TypeCode.UInt32:
                    return rw.ReadToArray<UInt32>(groupId, name);

                case TypeCode.UInt64:
                    return rw.ReadToArray<UInt64>(groupId, name);

                case TypeCode.String:
                    return rw.ReadStrings(groupId, name).ToArray();

                default:
                    if (elementType == typeof(TimeSpan))
                    {
                        var tss = rw.ReadToArray<long>(groupId, name);
                        return tss.ConvertArray<long, TimeSpan>(tcks => new TimeSpan(tcks));
                    }
                    string str = "type is not supported: ";
                    throw new NotSupportedException(str + elementType.FullName);

            }
        }

    }
}