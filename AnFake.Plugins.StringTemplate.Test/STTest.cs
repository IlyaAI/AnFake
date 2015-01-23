using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.StringTemplate.Test
{
	[TestClass]
	// ReSharper disable InconsistentNaming
	public class STTest
	{
		[TestCategory("Unit")]
		[TestMethod]
		public void ST_should_use_context()
		{
			// arrange
			var ctx = new {Value = "Value"};

			// act
			var result = ST.Render("Value: \"$_.Value$\"", ctx);

			// assert
			Assert.AreEqual("Value: \"Value\"", result);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void ST_should_escape_html_tags()
		{
			// arrange
			var ctx = new {Value = "<value/>"};

			// act
			var result = ST.Render("Value: <b>\"$_.Value$\"</b>", ctx);

			// assert
			Assert.AreEqual("Value: <b>\"&lt;value/&gt;\"</b>", result);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void ST_should_escape_html_tags_on_nested_objects()
		{
			// arrange			
			var ctx = new
			{
				Nested = new
				{
					Value = "<value>"
				}
			};

			// act
			var result = ST.Render("$_.Nested.Value$", ctx);

			// assert				
			Assert.AreEqual("&lt;value&gt;", result);
		}
	}
	// ReSharper restore InconsistentNaming
}