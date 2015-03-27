using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace AnFake.Api
{
	/// <summary>
	///     Represents typed message of build trace.
	/// </summary>
	[DataContract(Name = "Generic", Namespace = "")]
	public sealed class TraceMessage : IFormattable
	{
		private List<Hyperlink> _links;

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
		public int NodeId { get; set; }

		[DataMember]
		public List<Hyperlink> Links
		{
			get { return _links ?? (_links = new List<Hyperlink>()); }
		}		

		/// <summary>
		///     Formats message.
		/// </summary>
		/// <remarks>
		///     <para>
		///         Message text is a default string representation. Additionally the following information might be included:
		///     </para>
		///		<para>
		///			m - message itself prefixed with node id and/or code (if any)
		///		</para>
		///     <para>
		///         l - link if specified;
		///     </para>
		///     <para>
		///         f - file/project reference if specified (for warning or error only);
		///     </para>
		///		<para>
		///         F - file/project reference if specified (for all levels);
		///     </para>
		///     <para>
		///         d - details if specified;
		///     </para>
		/// </remarks>
		/// <param name="format"></param>
		/// <param name="formatProvider"></param>
		/// <returns></returns>
		public string ToString(string format, IFormatProvider formatProvider)
		{
			const int ident = 2;
			var sb = new StringBuilder(512);

			foreach (var field in format)
			{
				switch (field)
				{					
					case 'm':
						if (sb.Length > 0) 
							sb.AppendLine();

						if (NodeId != 0)
							sb.Append(NodeId).Append("> ");

						if (!String.IsNullOrEmpty(Code))			
							sb.Append(Code).Append(": ");

						sb.Append(Message);
						break;

					case 'l':						
						if (_links == null)
							break;

						foreach (var link in _links)
						{
							if (sb.Length > 0)
								sb.AppendLine();
							
							sb.Append(' ', ident).Append(link);
						}
						break;

					case 'F':
					case 'f':
						if (field == 'F' || Level >= TraceMessageLevel.Warning)
						{
							if (!String.IsNullOrEmpty(File))
							{
								if (sb.Length > 0)
									sb.AppendLine();

								sb.Append(' ', ident).Append(File);
								if (Line > 0)
									sb.AppendFormat(" Ln: {0}", Line);

								if (Column > 0)
									sb.AppendFormat(" Col: {0}", Column);
							}

							if (!String.IsNullOrEmpty(Project))
							{
								if (sb.Length > 0)
									sb.AppendLine();

								sb.Append(' ', ident).Append(Project);
							}
						}
						break;

					case 'd':
						if (!String.IsNullOrWhiteSpace(Details))
						{
							if (sb.Length > 0)
								sb.AppendLine();

							sb.Append(Details);
						}
						break;
				}
			}

			return sb.ToString();
		}

		/// <summary>
		///		Formats message. Equals to <c>ToString(format, null)</c>
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		public string ToString(string format)
		{
			return ToString(format, null);
		}

		/// <summary>
		///     Formats message with default presentation 'mlfd'. <see cref="ToString(string,System.IFormatProvider)" />
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ToString("mlfd", null);
		}
	}
}