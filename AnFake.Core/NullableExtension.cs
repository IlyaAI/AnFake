namespace AnFake.Core
{
	public static class NullableExtension
	{
		public static long? AsNullable(this long value)
		{
			return value;
		}

		public static int? AsNullable(this int value)
		{
			return value;
		}

		public static long? AsNullableLong(this int value)
		{
			return value;
		}

		public static bool? AsNullable(this bool value)
		{
			return value;
		}
	}
}