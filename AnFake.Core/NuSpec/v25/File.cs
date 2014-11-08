using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v25
{
	[Serializable]	
	public sealed class File
	{
		public File()
		{
		}

		public File(string src, string target)
		{
			Src = src;
			Target = target;
		}

		[XmlAttribute(AttributeName = "src")]
		public string Src { get; set; }

		[XmlAttribute(AttributeName = "target")]
		public string Target { get; set; }
	}
}