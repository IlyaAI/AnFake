using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/FileSet", "Data/FileSet")]
	[TestClass]
	public class RobocopyTest
	{
		[TestCategory("Functional")]
		[TestMethod]
		public void Robocopy_should_copy_files()
		{
			// arrange
			Folders.Delete("Data/Robocopy");

			// act
			Robocopy.Copy(
				"Data/FileSet".AsPath(),
				"Data/Robocopy".AsPath(),
				"*.txt",
				p =>
				{
					p.ExcludeFiles = "file-1.txt";
					p.ExcludeFolders = "xdir*";
					p.Recursion = Robocopy.RecursionMode.All;
				});

			// assert
			Assert.IsTrue(File.Exists("Data/Robocopy/dir-A/file-4.txt"));
			Assert.IsTrue(File.Exists("Data/Robocopy/dir-B/file-5.txt"));
			Assert.IsFalse(File.Exists("Data/Robocopy/file-1.txt"));
			Assert.IsFalse(Directory.Exists("Data/Robocopy/dir-A/xdir-E"));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void Robocopy_should_support_long_paths()
		{
			// arrange
			var veryLongName = "VeryLongName-0123456789".AsPath();
			var longName = "LongName-0123456789".AsPath();
			var shortName = "ShortName".AsPath();
			var file = "file-01.txt";
			var folder = "folder-" + new string('z', 248 - longName.Full.Length - 9);

			Directory.CreateDirectory((longName / folder).Spec);			
			File.Create((longName / folder / file).Spec).Close();

			Folders.Delete("Data/LongNameTestRobocopy");

			// act I
			Robocopy.Copy(
				longName,
				veryLongName,
				p =>
				{
					p.Recursion = Robocopy.RecursionMode.All;
				});

			// act II			
			Robocopy.Copy(
				veryLongName,
				shortName,
				p =>
				{
					p.Recursion = Robocopy.RecursionMode.All;
					p.DeleteSourceFiles = true;
				});

			// assert
			Assert.IsTrue(File.Exists((shortName / folder / file).Spec));
		}
	}
}