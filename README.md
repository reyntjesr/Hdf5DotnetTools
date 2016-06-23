# Hdf5DotnetTools
Set of tools that help in reading and writing hdf5 files for .net environments

## Introduction
At the neurology department of the Leiden University Medical Centre in the Netherlands we needed to convert large medical data files to a format that could easily be used in programs like Matlab, R and Python.

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


## Other projects that use the HDF.Pinvoke library
Another project on github that uses the [HDF.Pinvoke](https://github.com/HDFGroup/HDF.PInvoke) library to read and write HDF5 files is the [sharpHDF](https://github.com/sharpHDF/sharpHDF) project. I discovered it while I was working on my own library. It has a different approah to writing and reading hdf5 files. You have to create a Hdf5File object and fill it with groups, attributes and datasets. When you close the Hdf5File object it writes the file.
