using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[TestClass]
	public class FileSystemPath
	{		
		[TestCategory("Functional")]
		[TestMethod]
		public void FileSystemPath_should_expand_wellknown_folders()
		{			
			// arrange
			
			// act
			var path = "[ProgramFilesx86]/Microsoft".AsPath();

			// assert
			Assert.AreEqual(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft"), path.Full);
		}		
	}
}