using System;

namespace AnFake.Core
{
	public static class ArgumentsBuilderExtension
	{
		public static ArgumentsBuilder Option(this ArgumentsBuilder args, string name, FileSystemPath path)
		{
			return args.Option(name, path != null ? path.Full : null);
		}

		public static ArgumentsBuilder Option(this ArgumentsBuilder args, string name, string[] values, string separator)
		{
			if (values == null || values.Length == 0)
				return args;

			return args.Space().ValuedOption(name).NonQuotedValue(String.Join(separator, values));
		}

		public static ArgumentsBuilder Option(this ArgumentsBuilder args, string name, int? value)
		{
			if (value == null)
				return args;

			return args.Option(name, value.Value);
		}
	}
}