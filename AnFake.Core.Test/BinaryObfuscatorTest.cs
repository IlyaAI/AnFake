using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/Obfuscation.txt", "Data")]
	[TestClass]
	public class BinaryObfuscatorTest
	{
		[TestCategory("Unit")]
		[TestMethod]
		public void Obfuscate_should_obfustace_file_content()
		{
			// arrange
			var srcFile = "Data/Obfuscation.txt".AsFile();
			var dstFile = "Data/Obfuscated.txt".AsFile();
			Files.Copy(srcFile, dstFile.Path);
			
			// act
			BinaryObfuscator.Obfuscate(dstFile);
			
			// assert
			var content = Text.ReadFrom(dstFile);
			Assert.IsFalse(content.Contains("OBFUSCATION"));
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void Unobfuscate_should_restore_file_content()
		{
			// arrange
			var srcFile = "Data/Obfuscation.txt".AsFile();
			var dstFile = "Data/Obfuscated.txt".AsFile();
			Files.Copy(srcFile, dstFile.Path);
			BinaryObfuscator.Obfuscate(dstFile);

			// act
			BinaryObfuscator.Unobfuscate(dstFile);

			// assert
			var content = Text.ReadFrom(dstFile);
			Assert.IsTrue(content.Contains("OBFUSCATION"));
		}
	}
}