using System.IO;
using log4net.Util;

namespace AnFake.log4net
{
	public sealed class BuildLogPattern : PatternConverter
	{
		internal static string LogFile { get; set; }

		protected override void Convert(TextWriter writer, object state)
		{
			writer.Write(LogFile);
		}
	}
}