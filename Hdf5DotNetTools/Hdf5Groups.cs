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
    public static partial class Hdf5
    {

        public static int CloseGroup(int groupId)
        {
            return H5G.close(groupId);
        }

        public static int CreateGroup(int groupId, string groupName)
        {
            int gid;
            if (GroupExists(groupId, groupName))
                gid = H5G.open(groupId, groupName);
            else
                gid = H5G.create(groupId, groupName);
            return gid;
        }

        /// <summary>
        /// creates a structure of groups at once
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static int CreateGroupRecursively(int groupId, string groupName)
        {
            IEnumerable<string> grps = groupName.Split('/');
            int gid=groupId;
            groupName = "";
            foreach (var name in grps)
            {
                groupName = string.Concat(groupName, "/", name);
                gid = CreateGroup(gid, groupName);
            }
            return gid;
        }

        public static bool GroupExists(int groupId, string groupName)
        {
            H5G.info_t info = new H5G.info_t();
            var gid = H5G.get_info_by_name(groupId, groupName, ref info);
            return gid == 0;
        }

    }
}
