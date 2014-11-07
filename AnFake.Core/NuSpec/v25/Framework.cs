using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v25
{
	[Serializable]
	public enum Framework
	{
		[XmlEnum("net40")] Net40
	}
}