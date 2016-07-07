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
            var attributes = Attribute.GetCustomAttributes(tyObject);
            foreach (Attribute attr in attributes)
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
            WriteHdf5Attributes(attributes, groupId, groupName);
            if (createGroupName)
                Hdf5.CloseGroup(groupId);
            return (writeValue);
        }

        private static void WriteHdf5Attributes(Attribute[] attributes, int groupId, string name, string datasetName = null)
        {
            foreach (Attribute attr in attributes)
            {
                name = name + "_attr";
                if (attr is Hdf5StringAttribute)
                {
                    var h5at = attr as Hdf5StringAttribute;
                    Hdf5.WriteStringAttribute(groupId, name, h5at.Name, datasetName);
                }
                if (attr is Hdf5StringAttributes)
                {
                    var h5ats = attr as Hdf5StringAttributes;
                    Hdf5.WriteStringAttributes(groupId, name, h5ats.Names, datasetName);
                }
            }
        }

        private static void WriteFields(Type tyObject, object writeValue, int groupId)
        {
            FieldInfo[] miMembers = tyObject.GetFields(BindingFlags.DeclaredOnly |
       /*BindingFlags.NonPublic |*/ BindingFlags.Instance | BindingFlags.Public);

            foreach (FieldInfo info in miMembers)
            {
                bool nextInfo = false;
                var attributes = Attribute.GetCustomAttributes(info);
                foreach (Attribute attr in attributes)
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
                    dsetRW.WriteArray(groupId, name, (Array)infoVal);
                else if (primitiveTypes.Contains(code) || ty == typeof(TimeSpan))
                    //WriteOneValue(groupId, name, infoVal);
                    CallByReflection(nameof(WriteOneValue), ty, new object[] { groupId, name, infoVal });
                else
                    WriteObject(groupId, infoVal, name);
                WriteHdf5Attributes(attributes, groupId, name, name);

            }
        }

        private static void WriteProperties(Type tyObject, object writeValue, int groupId)
        {
            PropertyInfo[] miMembers = tyObject.GetProperties(/*BindingFlags.DeclaredOnly |*/
       BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo info in miMembers)
            {
                bool nextInfo = false;
                var attributes = Attribute.GetCustomAttributes(info);
                foreach (Attribute attr in attributes)
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
                    dsetRW.WriteArray(groupId, name, (Array)infoVal);
                else if (primitiveTypes.Contains(code) || ty == typeof(TimeSpan))
                    //WriteOneValue(groupId, name, infoVal);
                    CallByReflection(nameof(WriteOneValue), ty, new object[] { groupId, name, infoVal });
                else
                    WriteObject(groupId, infoVal, name);
                WriteHdf5Attributes(attributes, groupId, name, name);
            }
        }
        static void CallByReflection(string name, Type typeArg,
                             object[] values)
        {
            // Just for simplicity, assume it's public etc
            MethodInfo method = typeof(Hdf5).GetMethod(name);
            MethodInfo generic = method.MakeGenericMethod(typeArg);
            generic.Invoke(null, values);
        }

    }
}
