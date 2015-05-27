using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AnFake.Core.Exceptions;
using Newtonsoft.Json;

namespace AnFake.Core
{
	/// <summary>
	///		Represents Name=Value style settings stored in JSON format.
	/// </summary>
	public sealed class Settings : IEnumerable<KeyValuePair<string, string>>
	{
		internal const string LocalPath = "AnFake.settings.json";
		internal const string UserPath = "[ApplicationData]/AnFake/settings.json";

		private static Settings _current;

		/// <summary>
		///		Current user's settings loaded from '[ApplicationData]/AnFake/settings.json'.
		/// </summary>
		/// <remarks>
		///		If no such settings file then an empty Settings object returned.
		/// </remarks>
		public static Settings Current
		{
			get { return _current ?? (_current = new Settings(UserPath.AsPath())); }
		}

		private readonly FileSystemPath _storePath;
		private IDictionary<string, string> _settings;

		/// <summary>
		///		Constructs new Settings instance from specified file.
		/// </summary>
		/// <remarks>
		///		If settings file doesn't exists then an empty Settings object constructed.
		/// </remarks>
		/// <param name="storePath">settings file to load from</param>
		public Settings(FileSystemPath storePath)
		{
			if (storePath == null)
				throw new ArgumentException("Settings(storePath): storePath must not be null");

			_storePath = storePath;

			Reload();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		///		Returns <c>KeyValuePair&lt;string, string></c> enumerator.
		/// </summary>
		/// <returns></returns>
		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return _settings.GetEnumerator();
		}

		/// <summary>
		///		Returns true if setting with specified name exists and false otherwise.
		/// </summary>
		/// <param name="name">setting name (not null or empty)</param>
		/// <returns>true if exists</returns>
		public bool Has(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Settings.Has(name): name must not be null or empty");

			string value;
			return _settings.TryGetValue(name, out value) && !String.IsNullOrEmpty(value);
		}

		/// <summary>
		///		Gets setting value by name.
		/// </summary>
		/// <remarks>
		///		If setting with specified name doesn't exists then exception is thrown.
		/// </remarks>
		/// <param name="name">setting name (not null or empty)</param>
		/// <returns>setting value</returns>
		public string Get(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Settings.Get(name): name must not be null or empty");

			string value;
			if (!_settings.TryGetValue(name, out value))
				throw new InvalidConfigurationException(String.Format("Setting property '{0}' is not defined.", name));
			if (String.IsNullOrEmpty(value))
				throw new InvalidConfigurationException(String.Format("Setting property '{0}' has empty value.", name));

			return value;
		}

		///  <summary>
		/// 		Gets setting value by name or default value if no such name.
		///  </summary>		
		///  <param name="name">setting name (not null or empty)</param>
		/// <param name="defaultValue">default value to be returned if setting doesn't exists</param>
		/// <returns>setting value</returns>
		public string Get(string name, string defaultValue)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Settings.Get(name, defaultValue): name must not be null or empty");

			string value;
			return _settings.TryGetValue(name, out value) && !String.IsNullOrEmpty(value)
				? value
				: defaultValue;
		}

		/// <summary>
		///		Sets setting value by name.
		/// </summary>
		/// <param name="name">setting name (not null or empty)</param>
		/// <param name="value">setting value</param>
		public void Set(string name, string value)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Settings.Set(name, value): name must not be null or empty");
			if (String.IsNullOrEmpty(value))
				throw new ArgumentException("Settings.Set(name, value): value must not be null or empty");

			_settings[name] = value;
		}

		/// <summary>
		///		Removes setting by name.
		/// </summary>
		/// <remarks>
		///		Does nothing if setting doesn't exists.
		/// </remarks>
		/// <param name="name">setting name (not null or empty)</param>
		public void Remove(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Settings.Remove(name): name must not be null or empty");			

			_settings.Remove(name);
		}

		/// <summary>
		///		Saves settings back file.
		/// </summary>
		/// <remarks>
		///		Settings are saved to the same file which they were loaded from.
		/// </remarks>
		public void Save()
		{
			var cfgFile = _storePath.AsFile();
			cfgFile.EnsurePath();

			using (var writer = new JsonTextWriter(File.CreateText(cfgFile.Path.Full)))
			{
				GetSerializer().Serialize(writer, _settings);
			}
		}

		/// <summary>
		///		Reloads settings from file.
		/// </summary>
		/// <remarks>
		///		Settings are reloaded from file which was specified in constructor.
		/// </remarks>
		public void Reload()
		{
			if (!_storePath.AsFile().Exists())
			{
				_settings = new Dictionary<string, string>();
			}
			else
			{				
				using (var reader = new JsonTextReader(File.OpenText(_storePath.Full)))
				{
					_settings = GetSerializer().Deserialize<Dictionary<string, string>>(reader);
				}
			}
		}

		private static JsonSerializer GetSerializer()
		{
			var serializer = new JsonSerializer
			{
				Formatting = Formatting.Indented				
			};

			return serializer;
		}
	}
}