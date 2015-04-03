using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[TestClass]
	public class FolderItemTest
	{
		[TestCategory("Unit")]
		[ExpectedException(typeof (ArgumentException))]
		[TestMethod]
		public void AsFolder_should_throw_if_wildcarded_path()
		{
			// arrange

			// act
			"/path/*".AsFolder();

			// assert			
		}
	}
}