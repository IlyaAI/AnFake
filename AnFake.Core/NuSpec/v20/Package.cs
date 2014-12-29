using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v20
{
	[Serializable]	
	[XmlRoot(ElementName = "package", Namespace = "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd")]
	public sealed class Package : IPackage
	{
		[XmlElement(ElementName = "metadata", IsNullable = false)]
		public Metadata Metadata { get; set; }

		[XmlArray("files")]
		[XmlArrayItem("file")]
		public File[] Files { get; set; }

		string IPackage.Id
		{
			get { return Metadata.Id; }
		}

		Version IPackage.Version
		{
			get { return Metadata.Version; }
		}

		void IPackage.Validate()
		{
			if (String.IsNullOrEmpty(Metadata.Id))
				throw new ArgumentException("NuSpec.v20.Metadata.Id must not be null or empty");

			if (Metadata.Version == null)
				throw new ArgumentException("NuSpec.v20.Metadata.Version must not be null");
			
			if (String.IsNullOrEmpty(Metadata.Authors))
				throw new ArgumentException("NuSpec.v20.Metadata.Authors must not be null or empty");
			
			if (String.IsNullOrEmpty(Metadata.Description))
				throw new ArgumentException("NuSpec.v20.Metadata.Description must not be null or empty");
		}
	}
}