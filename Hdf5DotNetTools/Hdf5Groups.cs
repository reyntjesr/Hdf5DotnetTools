using HDF.PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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
            hid_t gid = groupOrfileId;
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

        public static string[] GroupGroups(hid_t groupId)
        {
            string[] grps = new string[0];
            try
            {
                var buf_size = H5I.get_name(groupId, (StringBuilder)null, IntPtr.Zero) + 1;
                int len = buf_size.ToInt32() - 1;
                H5G.info_t g_info = new H5G.info_t();
                H5O.info_t o_info = new H5O.info_t();
                o_info = Hdf5.GroupInfo(groupId);

                var gid = H5G.get_info_by_name(groupId, ".", ref g_info);
                for (ulong i = 0; i < g_info.nlinks; i++)
                {
                    H5O.info_t info = new H5O.info_t();
                    gid = H5O.get_info_by_idx(groupId, ".", H5.index_t.NAME, H5.iter_order_t.NATIVE, i, ref info);
                    if (info.type == H5O.type_t.GROUP)
                    {
                        Trace.WriteLine("");
                    }
                    //H5G.get_info_by_idx
                }
                StringBuilder nameBuilder = new StringBuilder(buf_size.ToInt32());
                IntPtr size = H5I.get_name(groupId, nameBuilder, buf_size);


                Trace.WriteLine(nameBuilder.ToString());
            }
            catch (Exception)
            {
            }
            return grps;
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
