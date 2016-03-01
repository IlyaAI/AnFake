using System;
using System.Collections.Generic;
using AnFake.Api;

namespace AnFake.Core.Integration.Tests
{
	/// <summary>
	///		Represents set of test results.
	/// </summary>
	public sealed class TestSet
	{
		private readonly List<TestResult> _tests = new List<TestResult>();
		private readonly List<Hyperlink> _links = new List<Hyperlink>();

		public TestSet(string name, string runnerType, FileItem traceFile, FolderItem attachmentsFolder)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("TestSet(name, runnerType, traceFile[, attachmentsFolder]): name must not be null or empty");

			if (String.IsNullOrEmpty(runnerType))
				throw new ArgumentException("TestSet(name, runnerType, traceFile[, attachmentsFolder]): runnerType must not be null or empty");

			if (traceFile == null)
				throw new ArgumentException("TestSet(name, runnerType, traceFile[, attachmentsFolder]): traceFile must not be null");			

			Name = name;
			RunnerType = runnerType;
			TraceFile = traceFile;
			AttachmentsFolder = attachmentsFolder;
		}

		public TestSet(string name, string runnerType, FileItem traceFile)
			: this(name, runnerType, traceFile, null)
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
		///		Test runner type (e.g. MsTest, NUnit, etc).
		/// </summary>
		public string RunnerType { get; private set; }

		/// <summary>
		///		List of test results.
		/// </summary>		
		public List<TestResult> Tests
		{
			get { return _tests; }
		}

		/// <summary>
		///		List of links to artifacts related to this test-set.
		///		E.g. build server might expose .trx file as build artifact and attach link to it.
		/// </summary>		
		public List<Hyperlink> Links
		{
			get { return _links; }
		}
	}
}