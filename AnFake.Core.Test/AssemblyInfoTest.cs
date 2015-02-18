using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/AssemblyInfo.test.cs", "Data")]
	[DeploymentItem("Data/AssemblyInfo.target.cs", "Data")]
	[TestClass]
	public class AssemblyInfoTest
	{
		[TestCleanup]
		public void Cleanup()
		{
			Target.Finalise();
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void AssemblyInfo_should_embed_all_non_null_properties()
		{
			// arrange
			Files.Copy("Data/AssemblyInfo.test.cs", "Data/AssemblyInfo.t1.cs", true);

			// act
			"Test".AsTarget().Do(() => AssemblyInfo.Embed(
				"Data/AssemblyInfo.t1.cs".AsFileSet(),
				p =>
				{
					p.Title = "AnFake.Core.Test";
					p.Description = "Unit-tests for\r\n\t AnFake.Core";
					p.Configuration = "Any CPU";
					p.Company = "\"MyCompany\"";
					p.Product = "AnFake \\Another F# Make\\";
					p.Copyright = "Copyright © Ilya A. Ivanov 2014";
					p.Trademark = "(none)";
					p.Culture = "en";
					p.Version = new Version(0, 9, 2, 5);
					p.FileVersion = new Version(0, 9, 2, 7);
				})).Run();

			// assert
			var actual = File.ReadAllText("Data/AssemblyInfo.t1.cs");
			var expected = File.ReadAllText("Data/AssemblyInfo.target.cs");

			Assert.AreEqual(expected, actual);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void AssemblyInfo_should_revert_changes()
		{
			// arrange
			Files.Copy("Data/AssemblyInfo.test.cs", "Data/AssemblyInfo.t2.cs", true);

			// act
			"Test".AsTarget().Do(() => AssemblyInfo.EmbedTemporary(			
				"Data/AssemblyInfo.t2.cs".AsFileSet(),
				p =>
				{
					p.Title = "AnFake.Core.Test";					
				}));

			// assert
			var actual = File.ReadAllText("Data/AssemblyInfo.t2.cs");
			var expected = File.ReadAllText("Data/AssemblyInfo.test.cs");

			Assert.AreEqual(expected, actual);
		}
	}
}
