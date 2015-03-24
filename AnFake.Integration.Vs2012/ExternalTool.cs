using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace AnFake.Integration.Vs2012
{
	public sealed class ExternalTool
	{
		private const string ToolTitle = "ToolTitle{0}";
		private const string ToolCmd = "ToolCmd{0}";
		private const string ToolDir = "ToolDir{0}";
		private const string ToolArg = "ToolArg{0}";
		private const string ToolOpt = "ToolOpt{0}";
		private const string ToolSourceKey = "ToolSourceKey{0}";
		private const string ToolTitlePkg = "ToolTitlePkg{0}";
		private const string ToolTitleResId = "ToolTitleResID{0}";

		public string Title { get; set; }

		public string Command { get; set; }

		public string InitialDirectory { get; set; }

		public string Arguments { get; set; }

		public int Options { get; set; }

		public string SourceKey { get; set; }

		public string TitlePackage { get; set; }

		public string TitleResourceId { get; set; }

		internal static List<ExternalTool> Read(RegistryKey key)
		{
			var toolsCount = (int)key.GetValue("ToolNumKeys", 0);

			var tools = new List<ExternalTool>(toolsCount);
			for (var index = 0; index < toolsCount; index++)
			{
				tools.Add(Read(key, index));
			}

			return tools;
		}

		internal static void Write(RegistryKey key, List<ExternalTool> tools)
		{
			var prevToolsCount = (int)key.GetValue("ToolNumKeys", 0);
			key.SetValue("ToolNumKeys", tools.Count, RegistryValueKind.DWord);

			var index = 0;
			for (; index < tools.Count; index++)
			{
				Write(tools[index], key, index);
			}

			for (; index < prevToolsCount; index++)
			{
				Remove(key, index);
			}
		}
		
		private static ExternalTool Read(RegistryKey key, int index)
		{
			return new ExternalTool
			{
				Title = key.GetValue(String.Format(ToolTitle, index), "").ToString(),
				Command = key.GetValue(String.Format(ToolCmd, index), "").ToString(),
				InitialDirectory = key.GetValue(String.Format(ToolDir, index), "").ToString(),
				Arguments = key.GetValue(String.Format(ToolArg, index), "").ToString(),
				Options = (int) key.GetValue(String.Format(ToolOpt, index), 0),
				SourceKey = key.GetValue(String.Format(ToolSourceKey, index), "").ToString(),
				TitlePackage = key.GetValue(String.Format(ToolTitlePkg, index), "").ToString(),
				TitleResourceId = key.GetValue(String.Format(ToolTitleResId, index), "").ToString(),
			};
		}		

		private static void Write(ExternalTool tool, RegistryKey key, int index)
		{
			key.SetValue(String.Format(ToolTitle, index), tool.Title, RegistryValueKind.String);
			key.SetValue(String.Format(ToolCmd, index), tool.Command, RegistryValueKind.String);
			key.SetValue(String.Format(ToolDir, index), tool.InitialDirectory, RegistryValueKind.String);
			key.SetValue(String.Format(ToolArg, index), tool.Arguments, RegistryValueKind.String);
			key.SetValue(String.Format(ToolOpt, index), tool.Options, RegistryValueKind.DWord);
			key.SetValue(String.Format(ToolSourceKey, index), tool.SourceKey, RegistryValueKind.String);
			key.SetValue(String.Format(ToolTitlePkg, index), tool.TitlePackage, RegistryValueKind.String);
			key.SetValue(String.Format(ToolTitleResId, index), tool.TitleResourceId, RegistryValueKind.String);
		}

		private static void Remove(RegistryKey key, int index)
		{
			key.DeleteValue(String.Format(ToolTitle, index));
			key.DeleteValue(String.Format(ToolCmd, index));
			key.DeleteValue(String.Format(ToolDir, index));
			key.DeleteValue(String.Format(ToolArg, index));
			key.DeleteValue(String.Format(ToolOpt, index));
			key.DeleteValue(String.Format(ToolSourceKey, index));
			key.DeleteValue(String.Format(ToolTitlePkg, index));
			key.DeleteValue(String.Format(ToolTitleResId, index));
		}
	}
}