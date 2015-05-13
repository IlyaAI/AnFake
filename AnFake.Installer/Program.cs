using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Packaging;
using System.Reflection;

namespace AnFake.Installer
{
	internal sealed class Program
	{
		public static void Main(string[] args)
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Welcome to AnFake Installer!");
			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.Gray;

			var dstPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"AnFake2");
			
			Unpack(dstPath);
			RunSetup(dstPath);

			Console.WriteLine("Press any key to close...");
			Console.ReadKey();
		}

		[SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
		private static void Unpack(string dstPath)
		{
			Console.WriteLine("Unpacking files to '{0}'...", dstPath);

			Directory.CreateDirectory(dstPath);

			var filesNum = 0;
			using (var pkgStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AnFake.Installer.AnFake.nupkg"))
			using (var pkg = Package.Open(pkgStream))
			{
				foreach (var part in pkg.GetParts())
				{
					var srcPath = part.Uri.ToString();
					if (!srcPath.StartsWith("/bin/", StringComparison.OrdinalIgnoreCase))
						continue;

					var dstFile = Path.Combine(dstPath, srcPath.Substring(5));
					var dstFolder = Path.GetDirectoryName(dstFile);

					Directory.CreateDirectory(dstFolder);

					using (var input = part.GetStream())
					using (var output = File.Create(dstFile))
					{
						input.CopyTo(output);
					}

					filesNum++;
				}
			}

			Console.WriteLine("{0} files unpacked.", filesNum);
			Console.WriteLine();
		}

		private static void RunSetup(string dstPath)
		{
			Console.WriteLine("Starting wizard...");
			
			var process = new Process
			{
				StartInfo =
				{
					FileName = Path.Combine(dstPath, "AnFake.exe"),
					Arguments = String.Format("[AnFakeExtras]/vs-setup.fsx Wizard Verbosity=Minimal"),
					UseShellExecute = false
				}
			};
			
			process.Start();
			process.WaitForExit();
		}
	}
}