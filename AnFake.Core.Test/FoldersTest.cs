using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/Folders", "Data/Folders")]
	[TestClass]
	public class FoldersTest
	{
		[TestCategory("Functional")]
		[TestMethod]
		public void FoldersCreate_should_do_nothing_if_exists()
		{
			// arrange
			Directory.CreateDirectory("Data/Folders/new-a");

			// act
			Folders.Create("Data/Folders/new-a");

			// assert
			Assert.IsTrue(Directory.Exists("Data/Folders/new-a"));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FoldersCreate_should_create_recursively()
		{
			// arrange
			if (Directory.Exists("Data/Folders/new-a"))
			{
				Directory.Delete("Data/Folders/new-a", true);
			}				

			// act
			Folders.Create("Data/Folders/new-a/new-b");

			// assert
			Assert.IsTrue(Directory.Exists("Data/Folders/new-a"));
			Assert.IsTrue(Directory.Exists("Data/Folders/new-a/new-b"));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FoldersDelete_should_delete_recursively()
		{
			// arrange
			Directory.CreateDirectory("Data/Folders/del-a/del-c");
			Directory.CreateDirectory("Data/Folders/del-b");
			
			var fi = new FileInfo("Data/Folders/del-a/del-c/file.txt");
			fi.Create().Close();
			fi.Attributes |= FileAttributes.Hidden|FileAttributes.ReadOnly;
			
			// act
			Folders.Delete("Data/Folders/del-a");

			// assert
			Assert.IsFalse(Directory.Exists("Data/Folders/del-a"));
			Assert.IsTrue(Directory.Exists("Data/Folders/del-b"));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FoldersDelete_should_retry_on_failure()
		{
			// arrange
			FileSystem.Defaults.RetryInterval = TimeSpan.FromMilliseconds(1000);
			Directory.CreateDirectory("Data/Folders/del-a");
						
			var stream = File.Create("Data/Folders/del-a/file-1.txt");
			File.Create("Data/Folders/del-a/file-2.txt").Close();

			new Thread(() =>
			{
				Thread.Sleep(1000);
				stream.Close();
			}).Start();

			// act
			Folders.Delete("Data/Folders/del-a");

			// assert
			Assert.IsFalse(Directory.Exists("Data/Folders/del-a"));			
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FoldersDelete_should_do_nothing_if_not_exists()
		{
			// arrange
			
			// act
			Folders.Delete("Data/Folders/del-e");

			// assert
			Assert.IsFalse(Directory.Exists("Data/Folders/del-e"));			
		}
	}
}