using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v25
{
	[Serializable]	
	public sealed class Metadata
	{
		[XmlElement("id", IsNullable = false)]
		public string Id { get; set; }

		[XmlElement("version", IsNullable = false)]
		public string Version { get; set; }

		[XmlElement("authors", IsNullable = false)]
		public string Authors { get; set; }

		[XmlElement("summary")]
		public string Summary { get; set; }

		[XmlElement("description", IsNullable = false)]
		public string Description { get; set; }

		[XmlElement("copyright")]
		public string Copyright { get; set; }

		// TODO: insert other

		[XmlArray("references")]
		[XmlArrayItem("group")]
		public ReferenceGroup[] ReferenceGroups { get; set; }		
	}
}