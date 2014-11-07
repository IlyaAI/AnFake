using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v25
{
	[Serializable]	
	public sealed class ReferenceGroup
	{
		[XmlAttribute(AttributeName = "targetFramework")]
		public Framework TargetFramework { get; set; }

		[XmlElement("reference")]
		public Reference[] References { get; set; }		
	}
}