using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v20
{
	[Serializable]	
	public sealed class Reference
	{
		[XmlAttribute(AttributeName = "file")]
		public string File { get; set; }
	}
}