using System;
using System.Collections.Generic;
using AnFake.Api;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	public static class Plugin
	{
		private static readonly IDictionary<Type, IPlugin> PluginInstances = new Dictionary<Type, IPlugin>();

		public static void Register<T>(T plugin)
			where T : IPlugin
		{
			var pluginType = typeof (T);

			if (PluginInstances.ContainsKey(pluginType))
				throw new InvalidConfigurationException(String.Format("Plugin '{0}' already registered.", pluginType.GetPluginName()));

			PluginInstances.Add(pluginType, plugin);

			Trace.InfoFormat("Plugged-in: {0}, {1}", pluginType.FullName, pluginType.Assembly.FullName);
		}

		public static T Get<T>()
			where T : IPlugin
		{
			var pluginType = typeof (T);
			IPlugin plugin;

			if (!PluginInstances.TryGetValue(pluginType, out plugin))
				throw new InvalidConfigurationException(String.Format("Plugin '{0}' not registered. Hint: probably, you forgot to call {0}.UseIt()", pluginType.GetPluginName()));

			return (T) plugin;
		}

		public static void Reset()
		{
			PluginInstances.Clear();
		}

		private static string GetPluginName(this Type pluginType)
		{
			var beg = pluginType.Name.StartsWith("I") ? 1 : 0;
			var length = pluginType.Name.EndsWith("Plugin")
				? pluginType.Name.Length - 6
				: pluginType.Name.Length;

			return pluginType.Name.Substring(beg, length);
		}
	}
}