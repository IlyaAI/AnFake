using System;
using System.IO;
using AnFake.Core.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[TestClass]
	public class FileSystemPathTest
	{
		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_normalize_path()
		{
			// act & assert
			Assert.AreEqual("\\root\\path", "/root/path".AsPath().Spec);
			Assert.AreEqual("root\\path\\", "root/path/".AsPath().Spec);
			Assert.AreEqual("file", "file".AsPath().Spec);
			Assert.AreEqual("", ".".AsPath().Spec);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_convert_to_unc()
		{
			// act & assert
			Assert.AreEqual(@"\\" + Environment.MachineName + @"\c$\root\path", "c:/root/path".AsPath().ToUnc().Spec);			
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_return_unc_if_already_unc()
		{
			// act & assert
			Assert.AreEqual(@"\\host\share", "//host/share".AsPath().ToUnc().Spec);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_throw_if_dot_step_used_1()
		{
			// act & assert
			try
			{
				"./current".AsPath();
				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}
		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_throw_if_dot_step_used_2()
		{
			// act & assert
			try
			{
				"/./current".AsPath();
				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_throw_if_dot_step_used_3()
		{
			// act & assert
			try
			{
				"current/.".AsPath();
				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_throw_if_dot_step_used_4()
		{
			// act & assert
			try
			{
				"current/./".AsPath();
				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_throw_if_dot_dot_step_used_1()
		{
			// act & assert
			try
			{
				"../current".AsPath();
				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}
		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_throw_if_dot_dot_step_used_2()
		{
			// act & assert
			try
			{
				"/../current".AsPath();
				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_throw_if_dot_dot_step_used_3()
		{
			// act & assert
			try
			{
				"current/..".AsPath();
				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_throw_if_dot_dot_step_used_4()
		{
			// act & assert
			try
			{
				"current/../".AsPath();
				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}

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
		public void FileSystemPath_should_return_empty_parent_on_file()
		{
			// arrange

			// act
			var parent = "file.txt".AsPath().Parent;

			// assert
			Assert.AreEqual("", parent.Spec);
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
		public void FileSystemPath_should_throw_when_getting_parent_on_driver_root()
		{
			// arrange
			var path = "C:/".AsPath();

			// act & assert
			try
			{
				// ReSharper disable once UnusedVariable
				var parent = path.Parent;
				
				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_throw_when_getting_parent_on_root()
		{
			// arrange
			var path = "/".AsPath();

			// act & assert
			try
			{
				// ReSharper disable once UnusedVariable
				var parent = path.Parent;

				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_throw_when_getting_parent_on_empty()
		{
			// arrange
			var path = "".AsPath();

			// act & assert
			try
			{
				// ReSharper disable once UnusedVariable
				var parent = path.Parent;

				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_return_root_on_file_in_root()
		{
			// arrange
			var path = "/file.txt".AsPath();

			// act 
			var parent = path.Parent;

			//assert
			Assert.AreEqual("\\", parent.Spec);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_return_drive_root_on_file_in_root()
		{
			// arrange
			var path = "c:/file.txt".AsPath();

			// act 
			var parent = path.Parent;

			//assert
			Assert.AreEqual("c:\\", parent.Spec);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_combine_with_upstep_relative_path()
		{
			// act & assert
			Assert.AreEqual("\\base\\sub-path", ("/base/path".AsPath() / "../sub-path").Spec);
			Assert.AreEqual("\\base\\sub-path", ("/base/step1/step2".AsPath() / "../../sub-path").Spec);
			Assert.AreEqual("\\sub-path", ("/base/path".AsPath() / "../../sub-path").Spec);
			Assert.AreEqual("sub-path", ("base/path".AsPath() / "../../sub-path").Spec);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_not_combine_with_upstep_in_middle_relative_path_1()
		{
			// arrange
			var basePath = "/base/path".AsPath();

			// act & assert
			try
			{
				// ReSharper disable once UnusedVariable
				var result = basePath / "/../subPath";

				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_not_combine_with_upstep_in_middle_relative_path_2()
		{
			// arrange
			var basePath = "/base/path".AsPath();

			// act & assert
			try
			{
				// ReSharper disable once UnusedVariable
				var result = basePath / "step/../subPath";

				Assert.Fail("InvalidConfigurationException is expected.");
			}
			catch (InvalidConfigurationException)
			{
				// it's expected
			}
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSystemPath_should_return_path_itself_if_empty_appended()
		{
			// arrange
			var path = "/some/path".AsPath();

			// act 
			var result = path/"";
			
			// assert
			Assert.AreEqual("\\some\\path", result.Spec);
		}
	}
}