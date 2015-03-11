namespace AnFake.Core
{
	public static class NullableExtension
	{
		/// <summary>
		///		Converts long to Nullable&lt;long&gt;.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static long? AsNullable(this long value)
		{
			return value;
		}

		/// <summary>
		///		Converts int to Nullable&lt;int&gt;.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static int? AsNullable(this int value)
		{
			return value;
		}

		/// <summary>
		///		Converts int to Nullable&lt;long&gt;.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static long? AsNullableLong(this int value)
		{
			return value;
		}

		/// <summary>
		///		Converts bool to Nullable&lt;bool&gt;.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool? AsNullable(this bool value)
		{
			return value;
		}
	}
}