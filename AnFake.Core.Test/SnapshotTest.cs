using System;
using System.IO;
using AnFake.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/AssemblyInfo.test.cs", "Data")]
	[TestClass]
	public class SnapshotTest
	{
		public ITracer PrevTracer;
		public ITracer Tracer;

		[TestInitialize]
		public void Initialize()
		{
			Tracer = MockRepository.GenerateMock<ITracer>();
			PrevTracer = Trace.Set(Tracer);
		}

		[TestCleanup]
		public void Cleanup()
		{
			Trace.Set(PrevTracer);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void Snapshot_should_restore_last_modified_time_and_attributes()
		{
			// arrange
			const string filePath = "Data/AssemblyInfo.test.cs";			
			var lastModified = new DateTime(2014, 1, 1, 12, 03, 05);
			var attributes = FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System;
			File.SetLastWriteTimeUtc(filePath, lastModified);
			File.SetAttributes(filePath, attributes);

			// act
			using (var snapshot = new Snapshot())
			{
				snapshot.Save(filePath.AsFile());

				File.SetAttributes(filePath, FileAttributes.Normal);
				File.Delete(filePath);				
				
				snapshot.Revert();
			}

			// assert
			Assert.IsTrue(File.Exists(filePath));
			Assert.AreEqual(lastModified, File.GetLastWriteTimeUtc(filePath));
			Assert.AreEqual(attributes, File.GetAttributes(filePath));

			Tracer.AssertWasNotCalled(x => x.Write(Arg<TraceMessage>.Matches(y => y.Level == TraceMessageLevel.Warning)));
		}
	}
}