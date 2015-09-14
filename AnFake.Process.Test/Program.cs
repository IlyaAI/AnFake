using System;
using System.Text;
using System.Threading;
using AnFake.Api;

namespace AnFake.Process.Test
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("THIS TOOL FOR TEST PURPOSE ONLY.");
				Console.WriteLine("USAGE: AnFake.Process.Test <options>");
				Console.WriteLine("  --out <some-text-for-stdout>");
				Console.WriteLine("  --err <some-text-for-stderr>");
				Console.WriteLine("  --wait <seconds-to-wait>");
				Console.WriteLine("  --exit <exit-code>");
				Console.WriteLine("  --log <level> <message>");
				return 0;
			}

			try
			{
				for (var i = 0; i < args.Length;)
				{
					var key = args[i++];
					string text;
					int val;

					switch (key)
					{
						case "--out":
							text = GetStringArgument(args, i++, "<some-text-for-stdout>");
							Console.WriteLine(text);
							break;
						case "--err":
							text = GetStringArgument(args, i++, "<some-text-for-stderr>");
							Console.Error.WriteLine(text);
							break;
						case "--wait":
							val = GetIntArgument(args, i++, "<seconds-to-wait>");
							Thread.Sleep(TimeSpan.FromSeconds(val));
							break;
						case "--log":
							val = GetIntArgument(args, i++, "<level>");
							text = GetStringArgument(args, i++, "<message>");
							new JsonFileTracer("process.log.jsx", true).Write(new TraceMessage((TraceMessageLevel)val, text));
							break;
						case "--exit":
							val = GetIntArgument(args, i, "<exit-code>");
							return val;
						case "--out-utf8":
							text = GetStringArgument(args, i++, "<some-utf8-text-for-stdout>");
							using (var stm = Console.OpenStandardOutput())
							{
								stm.WriteByte(0xEF);
								stm.WriteByte(0xBB);
								stm.WriteByte(0xBF);

								var buffer = Encoding.UTF8.GetBytes(text);
								stm.Write(buffer, 0, buffer.Length);
							}
							break;
						case "--err-utf8":
							text = GetStringArgument(args, i++, "<some-utf8-text-for-stderr>");
							using (var stm = Console.OpenStandardError())
							{								
								stm.WriteByte(0xEF);
								stm.WriteByte(0xBB);
								stm.WriteByte(0xBF);

								var buffer = Encoding.UTF8.GetBytes(text);
								stm.Write(buffer, 0, buffer.Length);
							}
							break;						
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

			return 0;
		}

		private static string GetStringArgument(string[] args, int index, string description)
		{
			if (index >= args.Length)
				throw new Exception(String.Format("Argument missed. Expected: {0}", description));

			return args[index];
		}

		private static int GetIntArgument(string[] args, int index, string description)
		{
			if (index >= args.Length)
				throw new Exception(String.Format("Argument missed. Expected: {0}", description));

			return Int32.Parse(args[index]);
		}
	}
}