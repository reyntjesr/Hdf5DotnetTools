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

        public static T ReadObject<T>(int groupId, T readValue, string groupName)
        {
            if (readValue == null)
            {
                throw new ArgumentNullException(nameof(readValue));
            }
            bool isGroupName = !string.IsNullOrWhiteSpace(groupName);
            if (isGroupName)
                groupId = H5G.open(groupId, groupName);

            Type tyObject = readValue.GetType();
            foreach (Attribute attr in Attribute.GetCustomAttributes(tyObject))
            {
                if (attr is Hdf5GroupName)
                    groupName = (attr as Hdf5GroupName).Name;
                if (attr is Hdf5SaveAttribute)
                {
                    Hdf5SaveAttribute atLeg = attr as Hdf5SaveAttribute;
                    if (atLeg.SaveKind == Hdf5Save.DoNotSave)
                        return readValue;
                }
            }


            ReadFields(tyObject, readValue, groupId);
            ReadProperties(tyObject, readValue, groupId);

            if (isGroupName)
                Hdf5.CloseGroup(groupId);
            return readValue;
        }

        public static T ReadObject<T>(int groupId, string groupName) where T: new()
        {
            T readValue = new T();
            return ReadObject<T>(groupId, readValue, groupName);
        }

        private static void ReadFields(Type tyObject, object readValue, int groupId)
        {
            FieldInfo[] miMembers = tyObject.GetFields(BindingFlags.DeclaredOnly |
       /*BindingFlags.NonPublic |*/ BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo info in miMembers)
            {
                bool nextInfo = false;
                foreach (Attribute attr in Attribute.GetCustomAttributes(info))
                {
                    if (attr is Hdf5SaveAttribute)
                    {
                        Hdf5Save kind = (attr as Hdf5SaveAttribute).SaveKind;
                        nextInfo = (kind == Hdf5Save.DoNotSave);
                    }
                    else
                        nextInfo = false;
                }
                if (nextInfo) continue;

                Type ty = info.FieldType;
                TypeCode code = Type.GetTypeCode(ty);

                string name = info.Name;

                if (ty.IsArray)
                {
                    Array values = dsetRW.ReadArray(ty, groupId, name);
                    info.SetValue(readValue, values);
                    //throw new Exception("Not implemented yet");
                }
                else if (primitiveTypes.Contains(code) || ty == typeof(TimeSpan))
                    {
                    Array values = dsetRW.ReadArray(ty, groupId, name);
                    // get first value depending on rank of the matrix
                    int[] first = new int[values.Rank].Select(f => 0).ToArray();
                    info.SetValue(readValue, values.GetValue(first));
                }
                else
                {
                    Object value = info.GetValue(readValue);
                    if (value != null)
                        ReadObject(groupId, value, name);
                }
            }
        }

        private static void ReadProperties(Type tyObject, object readValue, int groupId)
        {
            PropertyInfo[] miMembers = tyObject.GetProperties(/*BindingFlags.DeclaredOnly |*/
       BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo info in miMembers)
            {
                bool nextInfo = false;
                foreach (Attribute attr in Attribute.GetCustomAttributes(info))
                {
                    Hdf5Save kind = (attr as Hdf5SaveAttribute).SaveKind;
                    nextInfo = (kind == Hdf5Save.DoNotSave);
                }
                if (nextInfo) continue;
                Type ty = info.PropertyType;
                TypeCode code = Type.GetTypeCode(ty);
                string name = info.Name;

                if (ty.IsArray)
                {
                    object value = dsetRW.ReadArray(ty.GetElementType(), groupId, name);
                    info.SetValue(readValue, value, null);
                    //throw new Exception("Not implemented yet");
                }
                else if(primitiveTypes.Contains(code) || ty == typeof(TimeSpan))
                {
                    Array values = dsetRW.ReadArray(ty, groupId, name);
                    int[] first = new int[values.Rank].Select(f => 0).ToArray();
                    info.SetValue(readValue, values.GetValue(first));
                }
                else
                {
                    Object value = info.GetValue(readValue, null);
                    if (value != null)
                        ReadObject(groupId, value, name);
                }
            }
        }

        /*public static object ReadValue(Type type, string name, int groupId)
        {
            if (type == null) return null;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return Hdf5.ReadOneValue<bool>(groupId, name);

                case TypeCode.Byte:
                    return Convert.ToByte(Hdf5.ReadOneValue<UInt16>(groupId, name));

                case TypeCode.Char:
                    return Convert.ToChar(Hdf5.ReadOneValue<string>(groupId, name));

                case TypeCode.DateTime:
                    var ticks = Hdf5.ReadOneValue<long>(groupId, name);
                    return new DateTime(ticks);

                case TypeCode.Decimal:
                    string number = Hdf5.ReadOneValue<string>(groupId, name);
                    return Convert.ToDecimal(number);

                case TypeCode.Double:
                    return Hdf5.ReadOneValue<double>(groupId, name);

                case TypeCode.Int16:
                    return Hdf5.ReadOneValue<Int16>(groupId, name);

                case TypeCode.Int32:
                    return Hdf5.ReadOneValue<Int32>(groupId, name);

                case TypeCode.Int64:
                    return Hdf5.ReadOneValue<Int64>(groupId, name);

                case TypeCode.SByte:
                    return Convert.ToSByte(Hdf5.ReadOneValue<Int16>(groupId, name));

                case TypeCode.Single:
                    return Convert.ToSingle(Hdf5.ReadOneValue<double>(groupId, name));

                case TypeCode.UInt16:
                    return Hdf5.ReadOneValue<UInt16>(groupId, name);

                case TypeCode.UInt32:
                    return Hdf5.ReadOneValue<UInt32>(groupId, name);

                case TypeCode.UInt64:
                    return Hdf5.ReadOneValue<UInt64>(groupId, name);

                case TypeCode.String:
                    return Hdf5.ReadOneValue<string>(groupId, name);

                default:
                    if (type == typeof(TimeSpan))
                    {
                        var tsTicks = Hdf5.ReadOneValue<long>(groupId, name);
                        return new TimeSpan(tsTicks);
                    }
                    string str = "type is not supported: ";
                    throw new NotSupportedException(str + type.FullName);
            }
        }*/

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

        /*public static Array ReadArray(Type type, string name, int groupId)
        {
            if (type == null) return null;
            Type elementType = type.GetElementType();
            TypeCode ty = Type.GetTypeCode(elementType);

            if (!(elementType.IsPrimitive || elementType.Name == "String"))
                //foreach (object el in collection)
                //    WriteObject(groupId, el, name);
                throw new NotSupportedException("type is not supported: " + elementType.FullName);
            else
                switch (ty)
                {
                    case TypeCode.Boolean:
                        Array array = convert2DtoArray(Hdf5.ReadTmpDataset<UInt16>(groupId, name), Convert.ToBoolean);
                        return array;

                    case TypeCode.Byte:
                        return convert2DtoArray(Hdf5.ReadDataset<UInt16>(groupId, name), Convert.ToByte);

                    case TypeCode.Char:
                        return Hdf5.ReadStrings(groupId, name).Cast<char>().ToArray();

                    case TypeCode.DateTime:
                        var ticks = convert2DtoArray(Hdf5.ReadDataset<long>(groupId, name));
                        return ticks.Select(tc=> new DateTime(tc)).ToArray();

                    case TypeCode.Decimal:
                        return Hdf5.ReadStrings(groupId, name).Cast<decimal>().ToArray();

                    case TypeCode.Double:
                        return convert2DtoArray(Hdf5.ReadDataset<double>(groupId, name));

                    case TypeCode.Int16:
                        return convert2DtoArray(Hdf5.ReadDataset<Int16>(groupId, name));

                    case TypeCode.Int32:
                        return convert2DtoArray(Hdf5.ReadDataset<Int32>(groupId, name));

                    case TypeCode.Int64:
                        return convert2DtoArray(Hdf5.ReadDataset<Int64>(groupId, name));

                    case TypeCode.SByte:
                        return convert2DtoArray(Hdf5.ReadDataset<Int16>(groupId, name), Convert.ToSByte);

                    case TypeCode.Single:
                        return convert2DtoArray(Hdf5.ReadDataset<Double>(groupId, name),Convert.ToSingle);

                    case TypeCode.UInt16:
                        return convert2DtoArray(Hdf5.ReadDataset<UInt16>(groupId, name));

                    case TypeCode.UInt32:
                        return convert2DtoArray(Hdf5.ReadDataset<UInt32>(groupId, name));

                    case TypeCode.UInt64:
                        return convert2DtoArray(Hdf5.ReadDataset<UInt64>(groupId, name));

                    case TypeCode.String:
                        return Hdf5.ReadStrings(groupId, name).ToArray();

                    default:
                        if (elementType == typeof(TimeSpan))
                        {
                            var tss = convert2DtoArray(Hdf5.ReadDataset<long>(groupId, name));
                            return tss.Select(tcks => new TimeSpan(tcks)).ToArray();
                        }
                        string str = "type is not supported: ";
                        throw new NotSupportedException(str + elementType.FullName);
                }
        }*/
    }

}
