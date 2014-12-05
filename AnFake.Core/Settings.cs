using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	public sealed class Settings : IEnumerable<KeyValuePair<string, string>>
	{
		private const string StorePathDefault = "[ApplicationData]/AnFake/settings.json";

		private static Settings _current;

		public static Settings Current
		{
			get { return _current ?? (_current = new Settings(StorePathDefault.AsPath())); }
		}

		private readonly FileSystemPath _storePath;
		private IDictionary<string, string> _settings;

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

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return _settings.GetEnumerator();
		}

		public bool Has(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Settings.Has(name): name must not be null or empty");

			string value;
			return _settings.TryGetValue(name, out value) && !String.IsNullOrEmpty(value);
		}

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

		public string Get(string name, string defaultValue)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Settings.Get(name, defaultValue): name must not be null or empty");

			string value;
			return _settings.TryGetValue(name, out value) && !String.IsNullOrEmpty(value)
				? value
				: defaultValue;
		}

		public void Set(string name, string value)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Settings.Set(name, value): name must not be null or empty");
			if (String.IsNullOrEmpty(value))
				throw new ArgumentException("Settings.Set(name, value): value must not be null or empty");

			_settings[name] = value;
		}

		public void Remove(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Settings.Remove(name): name must not be null or empty");			

			_settings.Remove(name);
		}

		public void Save()
		{
			var cfgDir = _storePath.Parent.Full;
			if (!Directory.Exists(cfgDir))
			{
				Directory.CreateDirectory(cfgDir);
			}			

			using (var stream = new FileStream(_storePath.Full, FileMode.Create, FileAccess.Write))
			{
				new DataContractJsonSerializer(
					typeof (Dictionary<string, string>),
					new DataContractJsonSerializerSettings {UseSimpleDictionaryFormat = true}
					).WriteObject(stream, _settings);
			}
		}

		public void Reload()
		{
			if (!_storePath.AsFile().Exists())
			{
				_settings = new Dictionary<string, string>();
			}
			else
			{
				using (var stream = new FileStream(_storePath.Full, FileMode.Open, FileAccess.Read))
				{
					_settings = (IDictionary<string, string>) new DataContractJsonSerializer(
						typeof (Dictionary<string, string>),
						new DataContractJsonSerializerSettings {UseSimpleDictionaryFormat = true}
						).ReadObject(stream);
				}
			}
		}
	}
}