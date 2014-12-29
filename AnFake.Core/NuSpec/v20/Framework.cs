using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v20
{
	[Serializable]
	public enum Framework
	{
		[XmlEnum("net40")] Net40,
		[XmlEnum("net45")] Net45
	}
}