using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v20
{
	[Serializable]	
	public sealed class Dependency
	{
		[XmlAttribute(AttributeName = "id")]
		public string Id { get; set; }

		[XmlAttribute(AttributeName = "version")]
		public string Version { get; set; }		
	}
}