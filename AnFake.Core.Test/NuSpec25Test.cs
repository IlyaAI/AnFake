using System.IO;
using System.Text;
using System.Xml.Serialization;
using AnFake.Core.NuSpec.v25;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[TestClass]
	public class NuSpec25Test
	{
		[TestMethod]
		public void NuSpec25_model_should_be_serializable()
		{
			// arrange
			var pkg = new Package
			{
				Metadata = new Metadata
				{
					Id = "Id",
					ReferenceGroups = new[]
					{
						new ReferenceGroup
						{
							TargetFramework = Framework.Net40,
							References = new[]
							{
								new Reference {File = "file.dll"}
							}
						}
					}
				}
			};

			// act
			var sx = Serialize(pkg);

			// assert
			Assert.IsTrue(sx.Contains("<package "), "<package> tag not found");
			Assert.IsTrue(sx.Contains("<metadata>"), "<metadata> tag not found");
			Assert.IsTrue(sx.Contains("<id>Id</id>"), "<id> tag not found");
			Assert.IsTrue(sx.Contains("<references>"), "<references> tag not found");
			Assert.IsTrue(sx.Contains("<group targetFramework=\"net40\">"), "<group> tag not found");
			Assert.IsTrue(sx.Contains("<reference file=\"file.dll\" />"), "<reference> tag not found");			
		}

		[TestMethod]
		public void NuSpec25_model_should_be_deserializable()
		{
			// arrange
			var sx =
				"<?xml version=\"1.0\"?><package xmlns=\"http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd\"><metadata><id>Id</id><references><group targetFramework=\"net40\"><reference file=\"file.dll\"/></group></references></metadata></package>";

			// act
			var pkg = Deserialize(sx);

			// assert			
			Assert.AreEqual("Id", pkg.Metadata.Id);
			Assert.AreEqual(Framework.Net40, pkg.Metadata.ReferenceGroups[0].TargetFramework);
			Assert.AreEqual("file.dll", pkg.Metadata.ReferenceGroups[0].References[0].File);
		}

		private static string Serialize(Package obj)
		{
			using (var stm = new MemoryStream())
			{
				new XmlSerializer(typeof(Package)).Serialize(stm, obj);				

				return Encoding.UTF8.GetString(stm.ToArray());
			}
		}

		private static Package Deserialize(string sx)
		{
			using (var stm = new MemoryStream(Encoding.UTF8.GetBytes(sx)))
			{
				return (Package) new XmlSerializer(typeof(Package)).Deserialize(stm);
			}
		}
	}
}