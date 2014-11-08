using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[TestClass]
	public class FileSystemPath
	{		
		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_expand_wellknown_folders()
		{			
			// arrange
			
			// act
			var path = "[ProgramFilesx86]/Microsoft".AsPath();

			// assert
			Assert.AreEqual(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft"), path.Full);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_return_relative_parent_on_relative_path()
		{
			// arrange

			// act
			var parent = "relative/path".AsPath().Parent;

			// assert
			Assert.AreEqual("relative", parent.Spec);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_return_full_parent_on_full_path()
		{
			// arrange

			// act
			var parent = "c:/full/path".AsPath().Parent;

			// assert
			Assert.AreEqual("c:\\full", parent.Spec);
		}
	}
}