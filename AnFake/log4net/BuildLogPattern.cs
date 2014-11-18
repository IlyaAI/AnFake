using System.IO;
using log4net.Util;

namespace AnFake.log4net
{
	public sealed class BuildLogPattern : PatternConverter
	{
		internal static readonly string LogFile = Path.Combine(Directory.GetCurrentDirectory(), "build.log");

		protected override void Convert(TextWriter writer, object state)
		{
			writer.Write(LogFile);
		}

		public static void Touch()
		{			
		}
	}
}