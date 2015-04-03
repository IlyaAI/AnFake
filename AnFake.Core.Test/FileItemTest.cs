using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[TestClass]
	public class FileItemTest
	{
		[TestCategory("Unit")]
		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void AsFile_should_throw_if_wildcarded_path_1()
		{
			// arrange

			// act
			"file.?xt".AsFile();

			// assert			
		}

		[TestCategory("Unit")]
		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void AsFile_should_throw_if_wildcarded_path_2()
		{
			// arrange

			// act
			"path/*/file.txt".AsFile();

			// assert			
		}

		[TestCategory("Unit")]
		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void AsFile_should_throw_if_wildcarded_path_3()
		{
			// arrange

			// act
			"path/*/file.?xt".AsPath().AsFile();

			// assert			
		}

		[TestCategory("Unit")]
		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void AsFileFrom_should_throw_if_wildcarded_path_1()
		{
			// arrange

			// act
			"file.?xt".AsFileFrom("path");

			// assert			
		}

		[TestCategory("Unit")]
		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void AsFileFrom_should_throw_if_wildcarded_path_2()
		{
			// arrange

			// act
			"file.txt".AsFileFrom("path/*");

			// assert			
		}

		[TestCategory("Unit")]
		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void AsFileFrom_should_throw_if_wildcarded_path_3()
		{
			// arrange

			// act
			"file.?xt".AsPath().AsFileFrom("path");

			// assert			
		}

		[TestCategory("Unit")]
		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void AsFileFrom_should_throw_if_wildcarded_path_4()
		{
			// arrange

			// act
			"file.txt".AsFileFrom("path/*".AsPath());

			// assert			
		}
	}
}