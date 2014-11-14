using System;
using System.Globalization;

namespace AnFake.Api
{
	public static class ArgsExtension
	{
		public static Args Command(this Args args, string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Args.Command(name): name must not be null or empty");

			return args.Space().NonQuotedValue(name);
		}

		public static Args Param(this Args args, string value)
		{
			if (String.IsNullOrEmpty(value))
				throw new ArgumentException("Args.Param(value): value must not be null or empty");

			return args.Space().QuotedValue(value);
		}

		public static Args Other(this Args args, string other)
		{
			if (String.IsNullOrEmpty(other))
				return args;

			return args.Space().NonQuotedValue(other);
		}

		public static Args Option(this Args args, string name, string value)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Args.Option(name, value): name must not be null or empty");

			if (String.IsNullOrEmpty(value))
				return args;

			return args.Space().ValuedOption(name).QuotedValue(value);
		}

		public static Args Option(this Args args, string name, bool value)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Args.Option(name, value): name must not be null or empty");

			if (!value)
				return args;

			return args.Space().Option(name);
		}

		public static Args Option(this Args args, string name, int value)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Args.Option(name, value): name must not be null or empty");

			return args.Space().ValuedOption(name).NonQuotedValue(value.ToString(CultureInfo.InvariantCulture));
		}

		public static Args Option(this Args args, string name, Enum value)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Args.Option(name, value): name must not be null or empty");

			return args.Space().ValuedOption(name).NonQuotedValue(value.ToString());
		}		

		public static Args Option(this Args args, string name, string[] values, string separator)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Args.Option(name, value): name must not be null or empty");

			if (values == null || values.Length == 0)
				return args;

			return args.Option(name, String.Join(separator, values));
		}

		public static Args Option(this Args args, string name, int? value)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Args.Option(name, value): name must not be null or empty");

			if (value == null)
				return args;

			return args.Option(name, value.Value);
		}
	}
}