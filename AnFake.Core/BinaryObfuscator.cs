using System;
using System.Collections.Generic;
using System.IO;
using AnFake.Api;

namespace AnFake.Core
{
	public static class BinaryObfuscator
	{
		private static readonly byte[] ObfuscationPattern = { 0x0A, 0x32, 0xE9, 0x4D, 0xA8, 0xF5, 0x45, 0xD4, 0x86, 0xAC, 0x3A, 0x07, 0x6E, 0x98, 0xEB, 0xEC };

		public static void Obfuscate(IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new ArgumentException("BinaryObfuscator.Obfuscate(files): files must not be null");

			files = files.AsFormattable();			

			Trace.InfoFormat("BinaryObfuscator.Obfuscate: {{{0}}}", files.ToFormattedString());

			foreach (var file in files)
			{
				Trace.DebugFormat("  {0}", file);
				DoXoring(file);
			}
		}

		public static void Obfuscate(FileItem file)
		{
			if (file == null)
				throw new ArgumentException("BinaryObfuscator.Obfuscate(file): file must not be null");

			Trace.InfoFormat("BinaryObfuscator.Obfuscate: {0}", file);
			DoXoring(file);
		}

		public static void Unobfuscate(IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new ArgumentException("BinaryObfuscator.Unobfuscate(files): files must not be null");

			files = files.AsFormattable();

			Trace.InfoFormat("BinaryObfuscator.Unobfuscate: {{{0}}}", files.ToFormattedString());

			foreach (var file in files)
			{
				Trace.DebugFormat("  {0}", file);
				DoXoring(file);
			}
		}

		public static void Unobfuscate(FileItem file)
		{
			if (file == null)
				throw new ArgumentException("BinaryObfuscator.Unobfuscate(file): file must not be null");

			Trace.InfoFormat("BinaryObfuscator.Unobfuscate: {0}", file);
			DoXoring(file);
		}

		private static void DoXoring(FileItem file)
		{
			using (var stream = new FileStream(file.Path.Full, FileMode.Open, FileAccess.ReadWrite))
			{
				var buffer = new byte[64*1024];

				while (true)
				{
					var position = stream.Position;

					var bytesRead = stream.Read(buffer, 0, buffer.Length);
					if (bytesRead == 0)
						break;

					var patternIdx = position;
					for (var i = 0; i < bytesRead; i++, patternIdx++)
					{
						buffer[i] ^= ObfuscationPattern[patternIdx % ObfuscationPattern.Length];
					}

					stream.Position = position;
					stream.Write(buffer, 0, bytesRead);
				}
			}
		}
	}
}
