using System.Runtime.Serialization;

namespace AnFake.Api
{
	[DataContract]
	public enum TraceMessageLevel
	{
		[EnumMember]
		Debug,

		[EnumMember]
		Info,

		[EnumMember]
		Warning,

		[EnumMember]
		Error
	}
}