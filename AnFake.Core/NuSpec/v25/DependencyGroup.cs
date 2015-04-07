using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v25
{
	[Serializable]	
	public sealed class DependencyGroup
	{
		[XmlIgnore]
		public Framework? TargetFramework { get; set; }

		[XmlAttribute(AttributeName = "targetFramework")]
		// ReSharper disable once InconsistentNaming
		public string __TargetFramework
		{
			get
			{
				return TargetFramework != null 
					? TargetFramework.ToString().ToLowerInvariant() 
					: null;
			}
			set
			{
				TargetFramework = value != null
					? (Framework?) Enum.Parse(typeof(Framework), value, true)
					: null;
			}
		}

		[XmlElement("dependency")]
		public Dependency[] Dependencies{ get; set; }		
	}
}