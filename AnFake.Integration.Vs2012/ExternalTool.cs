using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace AnFake.Integration.Vs2012
{
	public sealed class ExternalTool
	{
		public const int OptionNone					= 0x00;
		public const int OptionCloseOnExit			= 0x02;
		public const int OptionPromptArgs			= 0x04;
		public const int OptionUseOutWindow			= 0x08;
		public const int OptionTreatOutAsUnicode	= 0x40;

		private const string ToolTitle = "ToolTitle{0}";
		private const string ToolCmd = "ToolCmd{0}";
		private const string ToolDir = "ToolDir{0}";
		private const string ToolArg = "ToolArg{0}";
		private const string ToolOpt = "ToolOpt{0}";
		private const string ToolSourceKey = "ToolSourceKey{0}";
		private const string ToolTitlePkg = "ToolTitlePkg{0}";
		private const string ToolTitleResId = "ToolTitleResID{0}";

		public ExternalTool()
		{
			Options = 0x10; // Unidentified default value
		}

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
				Title = GetStringValue(key, ToolTitle, index),
				Command = GetStringValue(key, ToolCmd, index),
				InitialDirectory = GetStringValue(key, ToolDir, index),
				Arguments = GetStringValue(key, ToolArg, index),
				Options = GetIntValue(key, ToolOpt, index),
				SourceKey = GetStringValue(key, ToolSourceKey, index),
				TitlePackage = GetStringValue(key, ToolTitlePkg, index),
				TitleResourceId = GetStringValue(key, ToolTitleResId, index)
			};
		}		

		private static void Write(ExternalTool tool, RegistryKey key, int index)
		{
			SetStringValue(key, ToolTitle, index, tool.Title);
			SetStringValue(key, ToolCmd, index, tool.Command);
			SetStringValue(key, ToolDir, index, tool.InitialDirectory);
			SetStringValue(key, ToolArg, index, tool.Arguments);
			SetIntValue(key, ToolOpt, index, tool.Options);
			SetStringValue(key, ToolSourceKey, index, tool.SourceKey);
			SetStringValue(key, ToolTitlePkg, index, tool.TitlePackage);
			SetStringValue(key, ToolTitleResId, index, tool.TitleResourceId);
		}

		private static void Remove(RegistryKey key, int index)
		{
			DeleteValue(key, ToolTitle, index);
			DeleteValue(key, ToolCmd, index);
			DeleteValue(key, ToolDir, index);
			DeleteValue(key, ToolArg, index);
			DeleteValue(key, ToolOpt, index);
			DeleteValue(key, ToolSourceKey, index);
			DeleteValue(key, ToolTitlePkg, index);
			DeleteValue(key, ToolTitleResId, index);
		}

		private static string GetStringValue(RegistryKey key, string name, int index)
		{
			var val = key.GetValue(String.Format(name, index), null);
			
			return val != null 
				? val.ToString() 
				: null;
		}

		private static int GetIntValue(RegistryKey key, string name, int index)
		{
			var val = key.GetValue(String.Format(name, index), null);

			return val != null
				? (int)val
				: 0;
		}

		private static void SetStringValue(RegistryKey key, string name, int index, string value)
		{
			if (value == null)
			{
				key.DeleteValue(String.Format(name, index), false);
			}
			else
			{
				key.SetValue(String.Format(name, index), value, RegistryValueKind.String);
			}
		}

		private static void SetIntValue(RegistryKey key, string name, int index, int value)
		{
			key.SetValue(String.Format(name, index), value, RegistryValueKind.DWord);
		}

		private static void DeleteValue(RegistryKey key, string name, int index)
		{			
			key.DeleteValue(String.Format(name, index), false);
		}
	}
}