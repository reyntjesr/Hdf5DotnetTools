# Hdf5DotnetTools
Set of tools that help in reading and writing hdf5 files for .net environments

## Introduction
At the neurology department of the Leiden University Medical Centre in the Netherlands we need to convert large medical data files to a format that could easily be used in programs like Matlab, R and Python.

## Usage

### write an object to an HDF5 file
In the example below an object is created with some arrays and other variables
The object is written to a file and than read back in a new object.

     private class TestClassWithArray
        {
            public double[] TestDoubles { get; set; }
            public string[] TestStrings { get; set; }
            public int TestInteger { get; set; }
            public double TestDouble { get; set; }
            public bool TestBoolean { get; set; }
            public string TestString { get; set; }
        }
     var testClass = new TestClassWithArray() {
                    TestInteger = 2,
                    TestDouble = 1.1,
                    TestBoolean = true,
                    TestString = "test string",
                    TestDoubles = new double[] { 1.1, 1.2, -1.1, -1.2 },
                    TestStrings = new string[] { "one", "two", "three", "four" }
        };
    int fileId = Hdf5.CreateFile("testFile.H5");

    Hdf5.WriteObject(fileId, testClass, "testObject");

    TestClassWithArray readObject = new TestClassWithArray();

    readObject = Hdf5.ReadObject(fileId, readObject, "testObject");

    Hdf5.CloseFile(fileId);

## Write a dataset and append new data to it

    /// <summary>
    /// create a matrix and fill it with numbers
    /// </summary>
    /// <param name="offset"></param>
    /// <returns>the matrix </returns>
    private static double[,]createDataset(int offset = 0)
    {
      var dset = new double[10, 5];
      for (var i = 0; i < 10; i++)
        for (var j = 0; j < 5; j++)
        {
          double x = i + j * 5 + offset;
          dset[i, j] = (j == 0) ? x : x / 10;
        }
      return dset;
    }
    
    // create a list of matrices
    dsets = new List<double[,]> {
                createDataset(),
                createDataset(10),
                createDataset(20) };
    
    string filename = Path.Combine(folder, "testChunks.H5");
    int fileId = Hdf5.CreateFile(filename);    
    
    // create a dataset and append two more datasets to it
    using (var chunkedDset = new ChunkedDataset<double>("/test", fileId, dsets.First()))
    {
      foreach (var ds in dsets.Skip(1))
        chunkedDset.AppendDataset(ds);
    }
    
    // read rows 9 to 22 of the dataset
    ulong begIndex = 8;
    ulong endIndex = 21;
    var dset = Hdf5.ReadDataset<double>(fileId, "/test", begIndex, endIndex);
    Hdf5.CloseFile(fileId);
    
## ToDo
This is a very early version of the library. Reading and writing attributes is still missing. I tried writing a generic method that could write compound arrays but that still doesn't work.

## Other projects that use the HDF.Pinvoke library
Another project on github that uses the [HDF.Pinvoke](https://github.com/HDFGroup/HDF.PInvoke) library to read and write HDF5 files is the [sharpHDF](https://github.com/sharpHDF/sharpHDF) project. I discovered it while I was working on my own library. It has a different approah to writing and reading hdf5 files. You have to create a Hdf5File object and fill it with groups, attributes and datasets. When you close the Hdf5File object it writes the file.
