using System;

namespace AnFake.Core
{
	public static class UserInterop
	{
		public static string Prompt(string name, string hint = null)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("UserInterop.Prompt(name[, hint]): name must not be null or empty");

			var prevColor = Console.ForegroundColor;

			Console.WriteLine();

			if (!String.IsNullOrWhiteSpace(hint))
			{
				Console.ForegroundColor = ConsoleColor.DarkYellow;				
				Console.WriteLine(hint);
			}

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(name);
			Console.Write("> ");

			Console.ForegroundColor = ConsoleColor.White;
			var value = Console.ReadLine();
			Console.WriteLine();

			Console.ForegroundColor = prevColor;
			return value;
		}

		public static bool Confirm(string operation, string hint = null)
		{
			if (String.IsNullOrEmpty(operation))
				throw new ArgumentException("UserInterop.Confirm(operation[, hint]): message must not be null or empty");
			
			var prevColor = Console.ForegroundColor;

			Console.WriteLine();

			if (!String.IsNullOrWhiteSpace(hint))
			{
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine(hint);
			}

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(operation);

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write("Enter/Space = OK, Esc = CANCEL?");
			var keyInfo = Console.ReadKey();			
			Console.WriteLine();
			
			Console.ForegroundColor = prevColor;

			return keyInfo.Key != ConsoleKey.Escape;
		}

		public static void Highlight(string name, string value, string hint = null)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("UserInterop.Print(name, value[, hint]): name must not be null or empty");

			if (value == null)
				throw new ArgumentException("UserInterop.Print(name, value[, hint]): value must not be null");

			var prevColor = Console.ForegroundColor;

			Console.WriteLine();

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(name);
			Console.Write(": ");

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(value);

			if (!String.IsNullOrWhiteSpace(hint))
			{
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine(hint);
				Console.WriteLine();
			}

			Console.ForegroundColor = prevColor;
		}
	}
}