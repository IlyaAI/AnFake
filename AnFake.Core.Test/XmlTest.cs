using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/mstest-01.trx", "Data")]
	[TestClass]
	public class XmlTest
	{
		[TestMethod]
		public void XNode_should_return_value_by_rooted_xpath_against_doc()
		{
			// arrange
			var xdoc = "Data/mstest-01.trx".AsFile().AsXmlDoc();

			// act
			var value = xdoc
				.Ns("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")		
				.ValueOf("/t:TestRun/t:TestSettings/t:Execution/t:TestTypeSpecific/t:UnitTestRunConfig/@testTypeId");

			// assert
			Assert.AreEqual("13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b", value);
		}

		[TestMethod]
		public void XNode_should_return_value_by_xpath_against_subnode()
		{
			// arrange
			var xdoc = "Data/mstest-01.trx".AsFile().AsXmlDoc();

			// act
			var value = xdoc
				.Ns("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
				.SelectFirst("/t:TestRun/t:TestSettings")
				.ValueOf("t:Execution/t:TestTypeSpecific/t:UnitTestRunConfig/@testTypeId");

			// assert
			Assert.AreEqual("13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b", value);
		}

		[TestMethod]
		public void XNode_should_return_value_by_rooted_xpath_against_subnode()
		{
			// arrange
			var xdoc = "Data/mstest-01.trx".AsFile().AsXmlDoc();

			// act
			var value = xdoc
				.Ns("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
				.SelectFirst("/t:TestRun/t:TestSettings")
				.ValueOf("/t:TestRun/t:TestSettings/t:Execution/t:TestTypeSpecific/t:UnitTestRunConfig/@testTypeId");

			// assert
			Assert.AreEqual("13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b", value);
		}

		[TestMethod]
		public void XNode_should_select_nodes_by_xpath()
		{
			// arrange
			var xdoc = "Data/mstest-01.trx".AsFile().AsXmlDoc();

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
			var xdoc = "Data/mstest-01.trx".AsFile().AsXmlDoc();

			// act
			var node = xdoc
				.Ns("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
				.Select("/t:TestRun/t:TestDefinitions/t:UnitTest")				
				.First();

			// assert
			Assert.AreEqual("TestDataNegativeTest", node.Attr("name"));
		}

		[TestMethod]
		public void XNode_should_return_namespaced_attribute_value()
		{
			// arrange
			var xdoc = AsXmlDoc("<root><element xmlns:t=\"urn:test:namespace\" t:attr=\"value\"/></root>");

			// act
			var value = xdoc
				.Ns("t", "urn:test:namespace")
				.SelectFirst("/root/element")
				.Attr("t:attr");

			// assert
			Assert.AreEqual("value", value);
		}

		[TestMethod]
		public void XNode_should_replace_attribute_value()
		{
			// arrange
			var xdoc = AsXmlDoc("<root><element attr=\"value\"/></root>");

			// act
			xdoc.SelectFirst("/root/element").SetAttr("attr", "new-value");

			// assert
			Assert.IsTrue(AsString(xdoc).Contains("attr=\"new-value\""));
		}

		[TestMethod]
		public void XNode_should_create_attribute_if_none()
		{
			// arrange
			var xdoc = AsXmlDoc("<root><element/></root>");

			// act
			xdoc.SelectFirst("/root/element").SetAttr("attr", "value");

			// assert
			Assert.IsTrue(AsString(xdoc).Contains("attr=\"value\""));
		}

		private static Xml.XDoc AsXmlDoc(string xml)
		{
			return (new MemoryStream(Encoding.UTF8.GetBytes(xml), false)).AsXmlDoc();
		}

		private static string AsString(Xml.XDoc xdoc)
		{
			using (var stream = new MemoryStream())
			{
				xdoc.SaveTo(stream);

				return Encoding.UTF8.GetString(stream.ToArray());
			}			
		}
	}
}