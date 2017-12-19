using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HDF.PInvoke;
using System.Runtime.InteropServices;
using System.IO;
using static HDF.PInvoke.H5T;

namespace Hdf5DotNetTools
{
#if HDF5_VER1_10
    using hid_t = System.Int64;
#else
    using hid_t = System.Int32;
#endif
    public class ChunkedDataset<T> : IDisposable where T : struct
    {
        ulong[] currentDims, oldDims;
        ulong[] maxDims = new ulong[] { H5S.UNLIMITED, H5S.UNLIMITED };
        ulong[] chunkDims;
        hid_t status, spaceId, datasetId, typeId, datatype, propId;

        /// <summary>
        /// Constructor to create a chuncked dataset object
        /// </summary>
        /// <param name="name"></param>
        /// <param name="groupId"></param>
        /// <param name="chunckSize"></param>
        public ChunkedDataset(string name, hid_t groupId, ulong[] chunckSize)
        {
            //Datasetname = Hdf5.ToHdf5Name(name);
            Datasetname = name;
            GroupId = groupId;
            datatype = Hdf5.GetDatatype(typeof(T));
            typeId = H5T.copy(datatype);
            chunkDims = chunckSize;
        }

        /// <summary>
        /// Constructor to create a chuncked dataset object with an initial dataset. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="groupId"></param>
        /// <param name="dataset"></param>
        public ChunkedDataset(string name, hid_t groupId, T[,] dataset) : this(name, groupId, new ulong[] { Convert.ToUInt64(dataset.GetLongLength(0)), Convert.ToUInt64(dataset.GetLongLength(1)) })
        {
            FirstDataset(dataset);
        }

        public void FirstDataset(Array dataset)
        {
            if (FalseGroupId) throw new Exception("cannot call FirstDataset because group or file couldn't be created");
            if (DatasetExists) throw new Exception("cannot call FirstDataset because dataset already exists");

            Rank = dataset.Rank;
            currentDims = GetDims(dataset);

            /* Create the data space with unlimited dimensions. */
            spaceId = H5S.create_simple(Rank, currentDims, maxDims);

            /* Modify dataset creation properties, i.e. enable chunking  */
            propId = H5P.create(H5P.DATASET_CREATE);
            status = H5P.set_chunk(propId, Rank, chunkDims);

            /* Create a new dataset within the file using chunk creation properties.  */
            datasetId = H5D.create(GroupId, Datasetname, datatype, spaceId, H5P.DEFAULT, propId, H5P.DEFAULT);

            /* Write data to dataset */
            GCHandle hnd = GCHandle.Alloc(dataset, GCHandleType.Pinned);
            status = H5D.write(datasetId, datatype, H5S.ALL, H5S.ALL, H5P.DEFAULT,
                hnd.AddrOfPinnedObject());
            hnd.Free();
            H5S.close(spaceId);
        }

        public void AppendDataset(Array dataset)
        {
            if (!DatasetExists) throw new Exception("call constructor or FirstDataset first before appending.");
            oldDims = currentDims;
            currentDims = GetDims(dataset);
            int rank = dataset.Rank;
            ulong[] zeros = Enumerable.Range(0, rank).Select(z => (ulong)0).ToArray();

            /* Extend the dataset. Dataset becomes 10 x 3  */
            var size = new ulong[] { oldDims[0] + currentDims[0] }.Concat(oldDims.Skip(1)).ToArray();

            status = H5D.set_extent(datasetId, size);
            ulong[] offset = new ulong[] { oldDims[0]}.Concat(zeros.Skip(1)).ToArray();

            /* Select a hyperslab in extended portion of dataset  */
            var filespaceId = H5D.get_space(datasetId);
            status = H5S.select_hyperslab(filespaceId, H5S.seloper_t.SET, offset, null,
                                          currentDims, null);

            /* Define memory space */
            var memId = H5S.create_simple(Rank, currentDims, null);

            /* Write the data to the extended portion of dataset  */
            GCHandle hnd = GCHandle.Alloc(dataset, GCHandleType.Pinned);
            status = H5D.write(datasetId, datatype, memId, filespaceId,
                               H5P.DEFAULT, hnd.AddrOfPinnedObject());
            hnd.Free();

            currentDims = size;
            H5S.close(memId);
            H5S.close(filespaceId);
        }

        /// <summary>
        /// Finalizer of object
        /// </summary>
        ~ChunkedDataset()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose function as suggested in the stackoverflow discussion below
        /// See: http://stackoverflow.com/questions/538060/proper-use-of-the-idisposable-interface/538238#538238
        /// </summary>
        /// <param name="itIsSafeToAlsoFreeManagedObjects"></param>
        protected virtual void Dispose(bool itIsSafeToAlsoFreeManagedObjects)
        {
            H5D.close(datasetId);
            H5P.close(propId);
            H5S.close(spaceId);

            if (itIsSafeToAlsoFreeManagedObjects)
            {

            }
        }

        private ulong[] GetDims(Array dset)
        {
            return Enumerable.Range(0, dset.Rank).Select(i =>
            { return (ulong)dset.GetLength(i); }).ToArray();
        }

        /// <summary>
        /// Dispose function as suggested in the stackoverflow discussion below
        /// See: http://stackoverflow.com/questions/538060/proper-use-of-the-idisposable-interface/538238#538238
        /// </summary>
        public void Dispose()
        {
            Dispose(true); //I am calling you from Dispose, it's safe
            GC.SuppressFinalize(this); //Hey, GC: don't bother calling finalize later
        }

        public string Datasetname { get; private set; }
        public int Rank { get; private set; }
        public hid_t GroupId { get; private set; }
        protected bool DatasetExists => H5L.exists(GroupId, Datasetname) > 0;
        protected bool FalseGroupId => GroupId <= 0;
    }
}
