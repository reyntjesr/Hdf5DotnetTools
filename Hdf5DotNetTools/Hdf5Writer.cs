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
        

        public static object WriteObject(int groupId, object writeValue, string groupName = null)
        {
            if (writeValue == null)
            {
                throw new ArgumentNullException(nameof(writeValue));
            }

            bool createGroupName = !string.IsNullOrWhiteSpace(groupName);
            if (createGroupName)
                groupId = Hdf5.CreateGroup(groupId, groupName);

            Type tyObject = writeValue.GetType();
            foreach (Attribute attr in Attribute.GetCustomAttributes(tyObject))
            {
                Hdf5SaveAttribute legAt = attr as Hdf5SaveAttribute;
                if (legAt != null)
                {
                    Hdf5Save kind = legAt.SaveKind;
                    if (kind == Hdf5Save.DoNotSave)
                        return writeValue;
                }
            }

            WriteProperties(tyObject, writeValue, groupId);
            WriteFields(tyObject, writeValue, groupId);
            if (createGroupName)
                Hdf5.CloseGroup(groupId);
            return (writeValue);
        }

        private static void WriteFields(Type tyObject, object writeValue, int groupId)
        {
            FieldInfo[] miMembers = tyObject.GetFields(BindingFlags.DeclaredOnly |
       /*BindingFlags.NonPublic |*/ BindingFlags.Instance | BindingFlags.Public);

            foreach (FieldInfo info in miMembers)
            {
                bool nextInfo = false;
                foreach (Attribute attr in Attribute.GetCustomAttributes(info))
                {
                    var legAttr = attr as Hdf5SaveAttribute;
                    var kind = legAttr?.SaveKind;
                    nextInfo = (kind == Hdf5Save.DoNotSave);
                }
                if (nextInfo) continue;
                object infoVal = info.GetValue(writeValue);
                if (infoVal == null)
                    continue;
                string name = info.Name;
                Type ty = infoVal.GetType();
                TypeCode code = Type.GetTypeCode(ty);

                if (ty.IsArray)
                    //throw new Exception("Not implemented yet");
                    WriteArray(groupId, name, (Array)infoVal);
                else if (primitiveTypes.Contains(code) || ty==typeof(TimeSpan))
                    WriteValue(groupId, name, infoVal);
                else
                    WriteObject(groupId, infoVal, name);
            }
        }

        private static void WriteProperties(Type tyObject, object writeValue, int groupId)
        {
            PropertyInfo[] miMembers = tyObject.GetProperties(/*BindingFlags.DeclaredOnly |*/
       BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo info in miMembers)
            {
                bool nextInfo = false;
                foreach (Attribute attr in Attribute.GetCustomAttributes(info))
                {
                    var legAttr = attr as Hdf5SaveAttribute;
                    var kind = legAttr?.SaveKind;
                    nextInfo = (kind == Hdf5Save.DoNotSave);
                }
                if (nextInfo) continue;
                object infoVal = info.GetValue(writeValue, null);
                if (infoVal == null)
                    continue;
                string name = info.Name;
                Type ty = infoVal.GetType();
                TypeCode code = Type.GetTypeCode(ty);

                if (ty.IsArray)
                    //throw new Exception("Not implemented yet");
                    WriteArray(groupId, name, (Array)infoVal);
                else if (primitiveTypes.Contains(code) || ty == typeof(TimeSpan))
                    WriteValue(groupId, name, infoVal);
                else
                    WriteObject(groupId, infoVal, name);
            }
        }

        private static void WriteValue(int groupId, string name, object prim)
        {
            Type type = prim.GetType();

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToBoolean(prim));
                    break;

                case TypeCode.Byte:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToUInt16(prim));
                    break;

                case TypeCode.Char:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToString(prim));
                    break;

                case TypeCode.DateTime:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToDateTime(prim).ToOADate());
                    break;

                case TypeCode.Decimal:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToDecimal(prim).ToString(CultureInfo.InvariantCulture));
                    break;

                case TypeCode.Double:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToDouble(prim));
                    break;

                case TypeCode.Int16:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToInt16(prim));
                    break;

                case TypeCode.Int32:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToInt32(prim));
                    break;

                case TypeCode.Int64:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToInt64(prim));
                    break;

                case TypeCode.SByte:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToInt16(prim));
                    break;

                case TypeCode.Single:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToDouble(prim));
                    break;

                case TypeCode.UInt16:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToUInt16(prim));
                    break;

                case TypeCode.UInt32:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToUInt32(prim));
                    break;

                case TypeCode.UInt64:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToUInt64(prim));
                    break;

                case TypeCode.String:
                    Hdf5.WritePrimitive(groupId, name, Convert.ToString(prim));
                    break;

                default:
                    if (type == typeof(TimeSpan))
                        Hdf5.WritePrimitive(groupId, name, ((TimeSpan)prim).Ticks);
                    else
                    {
                        //string str = SingletonConfiguration.Instance.RsrcMgr.GetString("notSuppExceptStr");
                        string str = "type is not supported: ";
                        throw new NotSupportedException(str + type.FullName);
                    }
                    break;
            }
        }

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

        private static void WriteArray(int groupId, string name, Array collection)
        {

            Type elementType = collection.GetType().GetElementType();
            TypeCode type = Type.GetTypeCode(elementType);

            //if (!(elementType.IsPrimitive || elementType.Name == "String"))
            //    throw new NotSupportedException("type is not supported: " + elementType.FullName);
            //if (!(elementType.IsPrimitive && elementType.IsClass))
            //    Hdf5.WriteCompounds(groupId, name, collection.OfType<object>());
           // else
                switch (type)
                {
                    case TypeCode.Boolean:
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<UInt16>(collection));
                        break;

                    case TypeCode.Byte:
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<UInt16>(collection));
                        break;

                    case TypeCode.Char:
                        Hdf5.WriteStrings(groupId, name, collection.OfType<object>().Select(o => o.ToString()));
                        break;

                    case TypeCode.DateTime:
                        var dts = collection.Cast<DateTime>().Select(dt => dt.ToOADate()).ToArray();
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<double>(dts));
                        break;

                    case TypeCode.Decimal:
                        var decs = collection.OfType<object>().Select(o => Convert.ToDecimal(o).ToString(CultureInfo.InvariantCulture));
                        Hdf5.WriteStrings(groupId, name, decs);
                        break;

                    case TypeCode.Double:
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<double>(collection));
                        break;

                    case TypeCode.Int16:
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<Int16>(collection));
                        break;

                    case TypeCode.Int32:
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<Int32>(collection));
                        break;

                    case TypeCode.Int64:
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<Int64>(collection));
                        break;

                    case TypeCode.SByte:
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<Int16>(collection));
                        break;

                    case TypeCode.Single:
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<double>(collection));
                        break;

                    case TypeCode.UInt16:
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<UInt16>(collection));
                        break;

                    case TypeCode.UInt32:
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<UInt32>(collection));
                        break;

                    case TypeCode.UInt64:
                        Hdf5.WriteDataset(groupId, name, convertArrayToType<UInt64>(collection));
                        break;

                    case TypeCode.String:
                        Hdf5.WriteStrings(groupId, name, collection.OfType<object>().Select(o => o.ToString()));
                        break;

                    default:
                        if (elementType == typeof(TimeSpan))
                        {
                            var ticks = collection.Cast<TimeSpan>().Select(t => t.Ticks).ToArray();
                            Hdf5.WriteDataset(groupId, name, convertArrayToType<Int64>(ticks));
                        }
                        else
                        {
                            string str = "type is not supported: ";
                            throw new NotSupportedException(str + elementType.FullName);
                        }
                        break;
                }

        }
    }
}
