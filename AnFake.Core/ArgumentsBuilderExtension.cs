using System;
using System.Globalization;

namespace AnFake.Core
{
	public static class ArgumentsBuilderExtension
	{
		public static ArgumentsBuilder Command(this ArgumentsBuilder args, string name)
		{
			return args.Space().NonQuotedValue(name);
		}

		public static ArgumentsBuilder Param(this ArgumentsBuilder args, string value)
		{
			return args.Space().QuotedValue(value);
		}

		public static ArgumentsBuilder Option(this ArgumentsBuilder args, string name, string value)
		{
			if (String.IsNullOrEmpty(value))
				return args;

			return args.Space().ValuedOption(name).QuotedValue(value);
		}

		public static ArgumentsBuilder Option(this ArgumentsBuilder args, string name, bool value)
		{
			if (!value)
				return args;

			return args.Space().Option(name);
		}

		public static ArgumentsBuilder Option(this ArgumentsBuilder args, string name, int value)
		{
			return args.Space().ValuedOption(name).NonQuotedValue(value.ToString(CultureInfo.InvariantCulture));
		}

		public static ArgumentsBuilder Option(this ArgumentsBuilder args, string name, Enum value)
		{
			return args.Space().ValuedOption(name).NonQuotedValue(value.ToString());
		}

		public static ArgumentsBuilder Option(this ArgumentsBuilder args, string name, FileSystemPath path)
		{
			return args.Option(name, path != null ? path.Full : null);
		}

		public static ArgumentsBuilder Option(this ArgumentsBuilder args, string name, string[] values, string separator)
		{
			if (values == null || values.Length == 0)
				return args;

			return args.Option(name, String.Join(separator, values));
		}

		public static ArgumentsBuilder Option(this ArgumentsBuilder args, string name, int? value)
		{
			if (value == null)
				return args;

			return args.Option(name, value.Value);
		}
	}
}