using System;
using System.Runtime.Serialization;
using System.Text;

namespace AnFake.Api
{
	/// <summary>
	/// Represents typed message of build trace.
	/// </summary>
	[DataContract(Name = "Generic", Namespace = "")]
	public class TraceMessage : IFormattable
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
		public string Code { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public string File { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public string Project { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public int Line { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public int Column { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public string Target { get; set; }
		
		[DataMember(EmitDefaultValue = false)]
		public string LinkHref { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public string LinkLabel { get; set; }

		/// <summary>
		/// Formats message.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Message text is a default string representation. Additionally the following information might be included:
		/// </para>
		/// <para>
		/// l - link if specified;
		/// </para>
		/// <para>
		/// f - file/project reference if specified;
		/// </para>
		/// <para>
		/// d - details if specified;
		/// </para>		
		/// </remarks>
		/// <param name="format"></param>
		/// <param name="formatProvider"></param>
		/// <returns></returns>
		public string ToString(string format, IFormatProvider formatProvider)
		{
			var sb = new StringBuilder();

			if (!String.IsNullOrEmpty(Code))
			{
				sb.Append(Code).Append(": ");
			}

			sb.Append(Message);

			foreach (var field in format)
			{
				switch (field)
				{
					case 'l':
						if (String.IsNullOrWhiteSpace(LinkHref))
							break;
						
						sb.AppendLine();
						if (!String.IsNullOrWhiteSpace(LinkLabel))
						{
							sb.Append('[').Append(LinkLabel).Append('|').Append(LinkHref).Append(']');
						}
						else
						{
							sb.Append('[').Append(LinkHref).Append(']');
						}
						break;

					case 'f':
						if (!String.IsNullOrEmpty(File))
						{
							sb.AppendLine().Append("    ").Append(File);
							if (Line > 0)
							{
								sb.AppendFormat(" Ln: {0}", Line);
							}
							if (Column > 0)
							{
								sb.AppendFormat(" Col: {0}", Column);
							}
						}

						if (!String.IsNullOrEmpty(Project))
						{
							sb.AppendLine().Append("    ").Append(Project);
						}
						break;

					case 'd':
						if (!String.IsNullOrWhiteSpace(Details))
						{
							sb.AppendLine().Append(Details);
						}
						break;
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Formats message with default presentation 'lfd'. <see cref="ToString(string,System.IFormatProvider)"/>
		/// </summary>		
		/// <returns></returns>
		public override string ToString()
		{
			return ToString("lfd", null);
		}
	}
}