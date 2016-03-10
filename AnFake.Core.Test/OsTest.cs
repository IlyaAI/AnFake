using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[TestClass]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class OsTest
	{
		[TestCategory("Unit")]
		[TestMethod]
		public void exe_should_add_exe_on_win()
		{
			// arrange
			Os.Type = Os.TypeName.Windows;

			// act & assert
			Assert.AreEqual("path/to/executable*.exe", Os.exe("path/to/executable*"));
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void dll_should_add_dll_on_win()
		{
			// arrange
			Os.Type = Os.TypeName.Windows;

			// act & assert
			Assert.AreEqual("path/to/dynamic-lib*.dll", Os.dll("path/to/dynamic-lib*"));
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void bat_should_add_bat_on_win()
		{
			// arrange
			Os.Type = Os.TypeName.Windows;

			// act & assert
			Assert.AreEqual("path/to/batch*.bat", Os.bat("path/to/batch*"));
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void exe_should_add_nothing_on_linux()
		{
			// arrange
			Os.Type = Os.TypeName.Linux;

			// act & assert
			Assert.AreEqual("path/to/executable*", Os.exe("path/to/executable*"));
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void dll_should_add_lib_and_so_on_linux()
		{
			// arrange
			Os.Type = Os.TypeName.Linux;

			// act & assert
			Assert.AreEqual("path/to/libdynamic-lib*.so", Os.dll("path/to/dynamic-lib*"));
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void bat_should_add_sh_on_linux()
		{
			// arrange
			Os.Type = Os.TypeName.Linux;

			// act & assert
			Assert.AreEqual("path/to/batch*.sh", Os.bat("path/to/batch*"));
		}
	}
}