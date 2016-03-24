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

			public Version VersionVal { get; set; }

			public IList<MySubObject> Children { get; set; }
		}

		public sealed class MySubObject
		{
			public string Value { get; set; }
		}

		public sealed class VersionObject
		{
			public Version Value { get; set; }
		}

		public sealed class DateObject
		{
			public DateTime Value { get; set; }
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
					"\"VersionVal\": \"1.2.3.4\"," +
					"\"DateVal\":\"2016-03-23T12:30:59\"," +
					"\"Children\": [" +
						"{\"Value\": \"child1\"}," +
						"{\"Value\": \"child2\"}" +
					"]" +
				"}";

			// act
			var obj = Json.ReadAs<MyObject>(json);

			// assert
			Assert.IsNotNull(obj);
			Assert.AreEqual(Int32.MaxValue, obj.IntVal);
			Assert.AreEqual(Int64.MaxValue, obj.LongVal);
			Assert.AreEqual((Double.MaxValue - 1e-15), obj.DoubleVal);
			Assert.AreEqual("string", obj.StringVal);
			Assert.AreEqual(new Version(1,2,3,4), obj.VersionVal);
			Assert.AreEqual(new DateTime(2016, 03, 23, 12, 30, 59), obj.DateVal);
			Assert.AreEqual(2, obj.Children.Count);
			Assert.AreEqual("child1", obj.Children[0].Value);
			Assert.AreEqual("child2", obj.Children[1].Value);
		}

		[TestMethod]
		[TestCategory("Functional")]
		public void Json_should_write_version_as_string()
		{
			// arrange
			var obj = new VersionObject {Value = new Version("1.2.3.4")};

			// act
			var json = Json.Write(obj);

			// arrange			
			Assert.IsNotNull(json);
			Assert.IsTrue(json.Contains("\"Value\":\"1.2.3.4\""));			
		}

		[TestMethod]
		[TestCategory("Functional")]
		public void Json_should_write_date_as_string()
		{
			// arrange
			var obj = new DateObject { Value = new DateTime(2016, 03, 23, 12, 30, 59) };

			// act
			var json = Json.Write(obj);

			// arrange			
			Assert.IsNotNull(json);
			Assert.IsTrue(json.Contains("\"Value\":\"2016-03-23T12:30:59\""));			
		}
	}
}