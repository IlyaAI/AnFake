using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v25
{
	[Serializable]	
	[XmlRoot(ElementName = "package", Namespace = "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd")]
	public sealed class Package
	{
		[XmlElement(ElementName = "metadata", IsNullable = false)]
		public Metadata Metadata { get; set; }

		[XmlArray("files")]
		[XmlArrayItem("file")]
		public File[] Files { get; set; }
	}
}