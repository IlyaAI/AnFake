using System;
using System.IO;
using System.Reflection;
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
			var path = "[ProgramFilesX86]/Microsoft".AsPath();

			// assert
			Assert.AreEqual(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft"), path.Full);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_expand_wellknown_folders_before_combining()
		{
			// arrange
			var basePath = "C:/System".AsPath();			

			// act
			var path = basePath / "[ProgramFilesX86]";

			// assert
			Assert.AreEqual(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), path.Full);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_expand_temp_folder()
		{
			// arrange

			// act
			var path = "[Temp]/Microsoft".AsPath();

			// assert
			Assert.AreEqual(Path.Combine(Path.GetTempPath(), "Microsoft"), path.Full);
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

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_resolve_relative_path()
		{
			// arrange
			var basePath = Assembly.GetExecutingAssembly().Location.AsPath();
			var filesPath = basePath / "Data/Files";
			var dataPath = filesPath / "..";

			// act
			var relative = dataPath.ToRelative(basePath/"Data");

			// assert
			Assert.AreEqual("", relative.Spec);
		}
	}
}