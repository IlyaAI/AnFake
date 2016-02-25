﻿using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

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
				throw new ArgumentException("Json.ReadAs(file): file must not be null");

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
				throw new ArgumentException("Json.ReadAs(value): value must not be null or empty");

			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(value), false))
			{
				return Deserialize<T>(stream);
			}
		}

		///  <summary>
		/// 	Writes object into JSON-file.
		///  </summary>
		/// <param name="obj">object to be written (not null)</param>
		/// <param name="file">json-file (not null)</param>		
		public static void Write(object obj, FileItem file)
		{
			if (obj == null)
				throw new ArgumentException("Json.Write(obj, file): obj must not be null");
			if (file == null)
				throw new ArgumentException("Json.Write(obj, file): file must not be null");

			using (var stream = new FileStream(file.Path.Full, FileMode.Create, FileAccess.Write))
			{
				Serialize(obj, stream);
			}
		}

		private static T Deserialize<T>(Stream stream)
		{			
			using (var streamReader = new StreamReader(stream))
			using (var jsonReader = new JsonTextReader(streamReader))
			{
				var serializer = new JsonSerializer();

				return serializer.Deserialize<T>(jsonReader);
			}			
		}

		private static void Serialize(object obj, Stream stream)
		{
			using (var streamWriter = new StreamWriter(stream))
			using (var jsonWriter = new JsonTextWriter(streamWriter))
			{
				new JsonSerializer().Serialize(jsonWriter, obj);
			}
		}
	}
}