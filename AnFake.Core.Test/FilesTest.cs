using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/Files", "Data/Files")]
	[TestClass]
	public class FilesTest
	{		
		[TestCategory("Functional")]
		[TestMethod]
		public void FilesDelete_should_retry_on_failure()
		{			
			// arrange
			FileSystem.Defaults.RetryInterval = TimeSpan.FromMilliseconds(1000);
			
			var stream = File.Create("Data/Files/file-1.txt");
			
			new Thread(() =>
			{
				Thread.Sleep(1000);
				stream.Close();
			}).Start();

			// act
			"Data/Files/file-1.txt".AsFile().Delete();

			// assert
			Assert.IsFalse(File.Exists("Data/Files/file-1.txt"));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FilesDelete_should_do_nothing_if_not_exists()
		{
			// arrange
			if (File.Exists("Data/Files/file-1.txt"))
			{
				File.Delete("Data/Files/file-1.txt");
			}			
			
			// act
			"Data/Files/file-1.txt".AsFile().Delete();

			// assert
			Assert.IsFalse(File.Exists("Data/Files/file-1.txt"));			
		}
	}
}