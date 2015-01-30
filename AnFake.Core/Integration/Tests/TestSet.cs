using System;
using System.Collections.Generic;

namespace AnFake.Core.Integration.Tests
{
	/// <summary>
	///		Represents set of test results.
	/// </summary>
	public sealed class TestSet
	{
		private readonly List<TestResult> _tests = new List<TestResult>();

		public TestSet(string name, FileItem traceFile, FolderItem attachmentsFolder)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("TestSet(name, traceFile[, attachmentsFolder]): name must not be null or empty");

			if (traceFile == null)
				throw new ArgumentException("TestSet(name, traceFile[, attachmentsFolder]): traceFile must not be null");

			Name = name;
			TraceFile = traceFile;
			AttachmentsFolder = attachmentsFolder;
		}

		public TestSet(string name, FileItem traceFile)
			: this(name, traceFile, null)
		{
		}

		/// <summary>
		///		Test set name (not null, read-only). Usually this is name of test assembly.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		///		Test trace file (not null, read-only).
		/// </summary>		
		public FileItem TraceFile { get; private set; }

		/// <summary>
		///		Test trace attachments folder (read-only).
		/// </summary>
		public FolderItem AttachmentsFolder { get; private set; }

		/// <summary>
		///		List of test results.
		/// </summary>		
		public List<TestResult> Tests
		{
			get { return _tests; }
		}
	}
}