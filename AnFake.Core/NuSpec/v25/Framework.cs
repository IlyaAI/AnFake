using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v25
{
	[Serializable]
	public enum Framework
	{
		//
		// IMPL. DETAILS: 
		// Names are important do not change.
		// See DependencyGroup.TargetFramework
		//
		[XmlEnum("net20")] Net20,
		[XmlEnum("net40")] Net40,
		[XmlEnum("net45")] Net45
	}
}