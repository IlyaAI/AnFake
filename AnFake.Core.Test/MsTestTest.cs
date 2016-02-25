using System;
using AnFake.Api;
using AnFake.Core.Integration.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/mstest-01.trx", "Data")]
	[DeploymentItem("Data/mstest-02.trx", "Data")]
	[TestClass]
	public class MsTestTest
	{
		public ITracer PrevTracer;		

		[TestInitialize]
		public void Initialize()
		{			
			PrevTracer = Trace.Set(MockRepository.GenerateMock<ITracer>());
		}

		[TestCleanup]
		public void Cleanup()
		{
			Trace.Set(PrevTracer);		
		}		

		[TestCategory("Functional")]
		[TestMethod]
		public void MsTestPostProcessor_should_parse_trx()
		{
			// arrange
			var pp = new MsTrxPostProcessor();

			// act
			var testSet = pp.PostProcess("Test", "Data/mstest-01.trx".AsFile());

			// assert
			Assert.AreEqual("Test", testSet.Name);
			Assert.AreEqual("Data\\mstest-01.trx", testSet.TraceFile.Path.Spec);
			Assert.AreEqual("Data\\Ivanov_I_IVANOV-I 2014-10-15 17_33_14", testSet.AttachmentsFolder.Path.Spec);

			var tests = testSet.Tests;
			Assert.AreEqual(2, tests.Count);
			Assert.AreEqual(TestStatus.Failed, tests[0].Status);
			Assert.AreEqual("AllInOne.TestDataTestSuite", tests[0].Suite);
			Assert.AreEqual("TestDataNegativeTest", tests[0].Name);
			Assert.IsFalse(String.IsNullOrWhiteSpace(tests[0].ErrorMessage));
			Assert.IsFalse(String.IsNullOrWhiteSpace(tests[0].ErrorStackTrace));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void MsTestPostProcessor_should_parse_trx_with_some_invalid_chars()
		{
			// arrange
			var pp = new MsTrxPostProcessor();

			// act
			var testSet = pp.PostProcess("Test", "Data/mstest-02.trx".AsFile());

			// assert
			var tests = testSet.Tests;
			Assert.AreEqual(1, tests.Count);
			Assert.IsNotNull(tests[0].Output);
			Assert.IsTrue(tests[0].Output.Contains("Invalid char here: \x0F"));
		}		
	}
}