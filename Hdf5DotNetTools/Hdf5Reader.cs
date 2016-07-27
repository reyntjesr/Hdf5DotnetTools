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
#if HDF5_VER1_10
    using hid_t = System.Int64;
#else
    using hid_t = System.Int32;
#endif
    public partial class Hdf5
    {

        public static T ReadObject<T>(hid_t groupId, T readValue, string groupName)
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

        public static T ReadObject<T>(hid_t groupId, string groupName) where T: new()
        {
            T readValue = new T();
            return ReadObject<T>(groupId, readValue, groupName);
        }

        private static void ReadFields(Type tyObject, object readValue, hid_t groupId)
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

        private static void ReadProperties(Type tyObject, object readValue, hid_t groupId)
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


    }

}
