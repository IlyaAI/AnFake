using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[TestClass]
	public class JsonTest
	{
		public sealed class MyObject
		{
			public int IntVal { get; set; }

			public long LongVal { get; set; }

			public double DoubleVal { get; set; }

			public string StringVal { get; set; }

			public DateTime DateVal { get; set; }

			public IList<MySubObject> Children { get; set; }
		}

		public sealed class MySubObject
		{
			public string Value { get; set; }
		}

		[TestMethod]
		[TestCategory("Functional")]
		public void Json_should_read_object_wo_attribute_markup()
		{
			// arrange
			var json =
				"{" +
					"\"IntVal\": " + Int32.MaxValue + "," +
					"\"LongVal\": " + Int64.MaxValue + "," +
					"\"DoubleVal\": 1.7976931348623157E+308," +
					"\"StringVal\": \"string\"," +
					// TODO: check date val
					"\"Children\": [" +
						"{\"Value\": \"child1\"}," +
						"{\"Value\": \"child2\"}" +
					"]" +
				"}";

			// act
			var obj = Json.Read<MyObject>(json);

			// assert
			Assert.IsNotNull(obj);
			Assert.AreEqual(Int32.MaxValue, obj.IntVal);
			Assert.AreEqual(Int64.MaxValue, obj.LongVal);
			Assert.AreEqual((Double.MaxValue - 1e-15), obj.DoubleVal);
			Assert.AreEqual("string", obj.StringVal);
			Assert.AreEqual(2, obj.Children.Count);
			Assert.AreEqual("child1", obj.Children[0].Value);
			Assert.AreEqual("child2", obj.Children[1].Value);
		}
	}
}