using System;
using System.Linq;
using System.Reflection;

namespace Hdf5DotNetTools
{
#if HDF5_VER1_10
    using hid_t = System.Int64;
#else
    using hid_t = System.Int32;
#endif
    public partial class Hdf5
    {


        public static object WriteObject(hid_t groupId, object writeValue, string groupName = null)
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
                if (attr is Hdf5SaveAttribute legAt)
                {
                    Hdf5Save kind = legAt.SaveKind;
                    if (kind == Hdf5Save.DoNotSave)
                        return writeValue;
                }
            }

            WriteProperties(tyObject, writeValue, groupId);
            WriteFields(tyObject, writeValue, groupId);
            WriteHdf5Attributes(tyObject, groupId, groupName);
            if (createGroupName)
                Hdf5.CloseGroup(groupId);
            return (writeValue);
        }

        private static void WriteHdf5Attributes(Type type, hid_t groupId, string name, string datasetName = null)
        {
            foreach (Attribute attr in Attribute.GetCustomAttributes(type))
            {
                if (attr is Hdf5Attribute)
                {
                    var h5at = attr as Hdf5Attribute;
                    WriteAttribute(groupId, name, h5at.Name, datasetName);
                }
                if (attr is Hdf5Attributes)
                {
                    var h5ats = attr as Hdf5Attributes;
                    WriteAttributes<string>(groupId, name, h5ats.Names, datasetName);
                }
            }
        }

        private static void WriteFields(Type tyObject, object writeValue, hid_t groupId)
        {
            FieldInfo[] miMembers = tyObject.GetFields(BindingFlags.DeclaredOnly |
       /*BindingFlags.NonPublic |*/ BindingFlags.Instance | BindingFlags.Public);

            foreach (FieldInfo info in miMembers)
            {
                if (NoSavePresent(Attribute.GetCustomAttributes(info))) continue;
                object infoVal = info.GetValue(writeValue);
                if (infoVal == null)
                    continue;
                string name = info.Name;
                //bool isEnumerable = info.FieldType.GetInterface(typeof(IEnumerable<>).FullName) != null;
                WriteField(infoVal, groupId, name);
            }
        }

        private static void WriteProperties(Type tyObject, object writeValue, hid_t groupId)
        {
            PropertyInfo[] miMembers = tyObject.GetProperties(
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo info in miMembers)
            {
                if (NoSavePresent(Attribute.GetCustomAttributes(info))) continue;
                object infoVal = info.GetValue(writeValue, null);
                if (infoVal == null)
                    continue;
                string name = info.Name;
                
                WriteField(infoVal, groupId, name);
            }
        }

        private static bool NoSavePresent(Attribute[] attributes)
        {
            bool noSaveAttr = false;
            foreach (Attribute attr in attributes)
            {
                var legAttr = attr as Hdf5SaveAttribute;
                var kind = legAttr?.SaveKind;
                if (kind == Hdf5Save.DoNotSave)
                {
                    noSaveAttr = true;
                    break;
                }
            }

            return noSaveAttr;
        }

        private static void WriteField(object infoVal, hid_t groupId, string name)
        {
            Type ty = infoVal.GetType();
            TypeCode code = Type.GetTypeCode(ty);

            if (ty.IsArray)
            {
                var elType = ty.GetElementType();
                TypeCode elCode = Type.GetTypeCode(elType);
                if (elCode != TypeCode.Object || ty == typeof(TimeSpan[]))
                    dsetRW.WriteArray(groupId, name, (Array)infoVal);
                else
                {
                    CallByReflection(nameof(WriteCompounds), elType, new object[] { groupId, name, infoVal });
                }
            }
            else if (primitiveTypes.Contains(code) || ty == typeof(TimeSpan))
                //WriteOneValue(groupId, name, infoVal);
                CallByReflection(nameof(WriteOneValue), ty, new object[] { groupId, name, infoVal });
            else
                WriteObject(groupId, infoVal, name);
        }

        static object CallByReflection(string name, Type typeArg,
                             object[] values)
        {
            // Just for simplicity, assume it's public etc
            MethodInfo method = typeof(Hdf5).GetMethod(name);
            MethodInfo generic = method.MakeGenericMethod(typeArg);
            return generic.Invoke(null, values);
        }

    }
}
