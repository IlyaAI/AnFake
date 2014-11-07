using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v25
{
	[Serializable]	
	public sealed class Metadata
	{
		[XmlElement("id")]
		public string Id { get; set; }

		public string Version { get; set; }

		public string Authors { get; set; }

		// TODO: insert other

		[XmlArray("references")]
		[XmlArrayItem("group")]
		public ReferenceGroup[] ReferenceGroups { get; set; }		
	}
}