using System;
using System.Runtime.Serialization;
using System.Text;

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

		[DataMember(EmitDefaultValue = false)]
		public string Details { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public string Target { get; set; }
		
		[DataMember(EmitDefaultValue = false)]
		public string LinkHref { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public string LinkLabel { get; set; }

		public override string ToString()
		{
			var sb = new StringBuilder()
				.Append(Message);

			if (!String.IsNullOrWhiteSpace(LinkHref))
			{
				sb.AppendLine();
				if (!String.IsNullOrWhiteSpace(LinkLabel))
				{
					sb.Append(LinkLabel).Append(" | ").Append(LinkHref);
				}
				else
				{
					sb.Append(LinkHref);
				}				
			}
			
			if (!String.IsNullOrWhiteSpace(Details))
			{
				sb.AppendLine().Append(Details);
			}

			return sb.ToString();
		}
	}
}