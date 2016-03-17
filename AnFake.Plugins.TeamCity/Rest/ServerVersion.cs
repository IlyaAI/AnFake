using System.Runtime.Serialization;

namespace AnFake.Plugins.TeamCity.Rest
{
	[DataContract]
	internal sealed class ServerVersion
	{
		[DataMember(Name = "version")]
		public string Version { get; set; }
	}
}