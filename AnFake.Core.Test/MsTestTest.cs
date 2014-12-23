using System;
using System.Linq;
using AnFake.Api;
using AnFake.Core.Integration.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/mstest-01.trx", "Data")]
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
			var tests = pp.PostProcess("Data/mstest-01.trx".AsPath()).ToList();

			// assert
			Assert.AreEqual(2, tests.Count);
			Assert.AreEqual(TestStatus.Failed, tests[0].Status);
			Assert.AreEqual("AllInOne.TestDataTestSuite", tests[0].Suite);
			Assert.AreEqual("TestDataNegativeTest", tests[0].Name);
			Assert.IsFalse(String.IsNullOrWhiteSpace(tests[0].ErrorMessage));
			Assert.IsFalse(String.IsNullOrWhiteSpace(tests[0].ErrorDetails));
		}
	}
}