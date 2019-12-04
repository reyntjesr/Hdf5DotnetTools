using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HDF.PInvoke;
using System.Runtime.InteropServices;
using System.IO;

namespace Hdf5DotNetTools
{
#if HDF5_VER1_10
    using hid_t = System.Int64;
#else
    using hid_t = System.Int32;
#endif
    public static partial class Hdf5
    {

        public static int CloseGroup(hid_t groupId)
        {
            return H5G.close(groupId);
        }

        public static hid_t CreateGroup(hid_t groupId, string groupName)
        {
            hid_t gid;
            if (GroupExists(groupId, groupName))
                gid = H5G.open(groupId, groupName);
            else
                gid = H5G.create(groupId, groupName);
            return gid;
        }

        /// <summary>
        /// creates a structure of groups at once
        /// </summary>
        /// <param name="groupOrfileId"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static hid_t CreateGroupRecursively(hid_t groupOrfileId, string groupName)
        {
            IEnumerable<string> grps = groupName.Split('/');
            hid_t gid=groupOrfileId;
            groupName = "";
            foreach (var name in grps)
            {
                groupName = string.Concat(groupName, "/", name);
                gid = CreateGroup(gid, groupName);
            }
            return gid;
        }

        public static bool GroupExists(hid_t groupId, string groupName)
        {
            bool exists = false;
            try
            {
                H5G.info_t info = new H5G.info_t();
                var gid = H5G.get_info_by_name(groupId, groupName, ref info);
                exists = gid == 0;
            }
            catch (Exception)
            {
            }
            return exists;
        }

        public static ulong NumberOfAttributes(int groupId, string groupName)
        {
            H5O.info_t info = new H5O.info_t();
            var gid = H5O.get_info(groupId, ref info);
            return info.num_attrs;
        }

        public static H5O.info_t GroupInfo(long groupId)
        {
            H5O.info_t info = new H5O.info_t();
            var gid = H5O.get_info(groupId, ref info);
            return info;
        }
    }
}
