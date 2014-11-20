using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/mstest-01.trx", "Data")]
	[TestClass]
	public class XmlTest
	{
		[TestMethod]
		public void XNode_should_return_value_by_xpath()
		{
			// arrange
			var xdoc = "Data/mstest-01.trx".AsPath().AsXmlDoc();

			// act
			var value = xdoc
				.Ns("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
				.ValueOf("/t:TestRun/t:TestSettings/t:Execution/t:TestTypeSpecific/t:UnitTestRunConfig/@testTypeId");

			// assert
			Assert.AreEqual("13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b", value);
		}

		[TestMethod]
		public void XNode_should_select_nodes_by_xpath()
		{
			// arrange
			var xdoc = "Data/mstest-01.trx".AsPath().AsXmlDoc();

			// act
			var nodes = xdoc
				.Ns("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
				.Select("/t:TestRun/t:TestDefinitions/t:UnitTest")
				.ToArray();

			// assert
			Assert.AreEqual(2, nodes.Length);
		}

		[TestMethod]
		public void XNode_should_return_attribute()
		{
			// arrange
			var xdoc = "Data/mstest-01.trx".AsPath().AsXmlDoc();

			// act
			var node = xdoc
				.Ns("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
				.Select("/t:TestRun/t:TestDefinitions/t:UnitTest")
				.First();

			// assert
			Assert.AreEqual("TestDataNegativeTest", node.Attr("name"));
		}
	}
}