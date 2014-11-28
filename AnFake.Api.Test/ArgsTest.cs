using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Api.Test
{
	[TestClass]
	public class ArgsTest
	{
		[TestMethod]
		public void Args_should_quote_value_1()
		{
			// arrange

			// act
			var args = new Args("", "").Param("no-spaces").ToString();

			// assert
			Assert.AreEqual("\"no-spaces\"", args);
		}

		[TestMethod]
		public void Args_should_quote_value_2()
		{
			// arrange

			// act
			var args = new Args("", "").Param("with spaces").ToString();

			// assert
			Assert.AreEqual("\"with spaces\"", args);
		}

		[TestMethod]
		public void Args_should_quote_value_3()
		{
			// arrange

			// act
			var args = new Args("", "").Param("with\" inside").ToString();

			// assert
			Assert.AreEqual("\"with\\\" inside\"", args);
		}

		[TestMethod]
		public void Args_should_quote_value_4()
		{
			// arrange

			// act
			var args = new Args("", "").Param("with\\\" inside").ToString();

			// assert
			Assert.AreEqual("\"with\\\\\\\" inside\"", args);
		}

		[TestMethod]
		public void Args_should_quote_value_5()
		{
			// arrange

			// act
			var args = new Args("", "").Param("with\\inside").ToString();

			// assert
			Assert.AreEqual("\"with\\inside\"", args);
		}

		[TestMethod]
		public void Args_should_quote_value_6()
		{
			// arrange

			// act
			var args = new Args("", "").Param("ending on\\").ToString();

			// assert
			Assert.AreEqual("\"ending on\\\\\"", args);
		}

		[TestMethod]
		public void Args_should_quote_value_7()
		{
			// arrange

			// act
			var args = new Args("", "").Param("\\root").ToString();

			// assert
			Assert.AreEqual("\"\\root\"", args);
		}

		[TestMethod]
		public void Args_should_quote_value_8()
		{
			// arrange

			// act
			var args = new Args("", "").Param("\\\\root\\path").ToString();

			// assert
			Assert.AreEqual("\"\\\\root\\path\"", args);
		}
	}
}