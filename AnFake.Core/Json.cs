using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AnFake.Core
{
	/// <summary>
	///		Represents JSON-related tools.
	/// </summary>
	public static class Json
	{
		/// <summary>
		///		Reads object from JSON-file.
		/// </summary>
		/// <typeparam name="T">type of object to be read</typeparam>
		/// <param name="file">json-file (not null)</param>
		/// <returns>deserialized object</returns>
		public static T ReadAs<T>(FileItem file)
			where T : class, new()
		{
			if (file == null)
				throw new ArgumentException("Json.Read(file): file must not be null");

			using (var stream = new FileStream(file.Path.Full, FileMode.Open, FileAccess.Read))
			{
				return Deserialize<T>(stream);
			}
		}

		/// <summary>
		///		Reads object from JSON-string.
		/// </summary>
		/// <typeparam name="T">type of object to be read</typeparam>
		/// <param name="value">json-string (not null or empty)</param>
		/// <returns>deserialized object</returns>
		public static T ReadAs<T>(string value)
			where T : class, new()
		{
			if (String.IsNullOrEmpty(value))
				throw new ArgumentException("Json.Read(value): value must not be null or empty");

			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(value), false))
			{
				return Deserialize<T>(stream);
			}
		}

		private static T Deserialize<T>(Stream stream)
		{
			return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
		}
	}
}