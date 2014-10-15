using System.Runtime.Serialization;

namespace AnFake.Api
{
	[DataContract]
	public enum TraceMessageLevel
	{
		[EnumMember]
		Info,

		[EnumMember]
		Warning,

		[EnumMember]
		Error
	}
}