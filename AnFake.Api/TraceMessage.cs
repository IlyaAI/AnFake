using System.Runtime.Serialization;

namespace AnFake.Api
{
	[DataContract(Name = "Generic", Namespace = "")]
	public class TraceMessage
	{
		public TraceMessage(TraceMessageLevel level, string message)
		{
			Level = level;
			Message = message;
		}

		[DataMember]
		public TraceMessageLevel Level { get; private set; }

		[DataMember]
		public string Message { get; private set; }

		[DataMember]
		public string Details { get; set; }
	}
}