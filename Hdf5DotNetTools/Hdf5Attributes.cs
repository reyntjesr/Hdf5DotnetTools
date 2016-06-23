using System;
using System.Collections.Generic;
using System.Linq;
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