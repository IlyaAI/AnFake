using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AnFake.Api;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	public static class AssemblyInfo
	{
		public sealed class Params
		{
			public string Title;
			public string Description;
			public string Configuration;
			public string Company;
			public string Product;
			public string Copyright;
			public string Trademark;
			public string Culture;
			public Version Version;
			public Version FileVersion;			

			internal Params()
			{				
			}

			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		private sealed class EmbedableAttribute
		{
			public readonly string Name;
			public readonly Func<Params, object> GetValue;

			public EmbedableAttribute(string name, Func<Params, object> getValue)
			{
				Name = name;
				GetValue = getValue;
			}
		}

		private static readonly EmbedableAttribute[] EmbedableAttributes = 
		{
			new EmbedableAttribute("AssemblyTitle", p => p.Title),
			new EmbedableAttribute("AssemblyDescription", p => p.Description),
			new EmbedableAttribute("AssemblyConfiguration", p => p.Configuration),
			new EmbedableAttribute("AssemblyCompany", p => p.Company),
			new EmbedableAttribute("AssemblyProduct", p => p.Product),
			new EmbedableAttribute("AssemblyCopyright", p => p.Copyright),
			new EmbedableAttribute("AssemblyTrademark", p => p.Trademark),			
			new EmbedableAttribute("AssemblyCulture", p => p.Culture),
			new EmbedableAttribute("AssemblyVersion", p => p.Version),
			new EmbedableAttribute("AssemblyFileVersion", p => p.FileVersion ?? p.Version)
		};

		private static readonly IDictionary<string, string> Patterns = new Dictionary<string, string>
		{
			{".cs", "^\\[assembly:\\s*{0}(?:Attribute)?\\s*\\(\\s*((?:@\\s*\".*?\")|(?:\"(?:[^\"\\\\]|\\\\.)*\"))\\s*\\)\\s*\\]\\s*$"}
		};

		private static readonly IDictionary<string, Regex> RegExps = new Dictionary<string, Regex>();

		public static Params Defaults { get; private set; }

		static AssemblyInfo()
		{
			Defaults = new Params();
		}

		public static AssemblyInfoExecutionResult EmbedTemporary(IEnumerable<FileItem> files, Action<Params> setParams)
		{
			var result = Embed(files, setParams);

			Target.CurrentFinalized += (s, r) => result.Revert();

			return result;
		}

		public static AssemblyInfoExecutionResult Embed(IEnumerable<FileItem> files, Action<Params> setParams)
		{
			// TODO: check args

			var parameters = Defaults.Clone();
			setParams(parameters);

			// TODO: check params

			var snapshot = new Snapshot();
			var warnings = 0;

			try
			{
				foreach (var file in files)
				{
					var content = File.ReadAllText(file.Path.Full);

					foreach (var attribute in EmbedableAttributes)
					{
						var value = attribute.GetValue(parameters);
						if (value == null)
							continue;

						try
						{
							EmbedAttribute(file.Ext.ToLowerInvariant(), attribute.Name, value.ToString(), ref content);
						}
						catch (AnFakeException e)
						{
							var msg = new TraceMessage(TraceMessageLevel.Warning, e.Message) { File = file.RelPath.Spec };
							Tracer.Write(msg);
							Logger.TraceMessage(msg);

							warnings++;
						}
					}

					snapshot.Save(file);

					File.WriteAllText(file.Path.Full, content);
				}
			}
			catch (Exception)
			{
				snapshot.Revert();
				throw;
			}

			return new AssemblyInfoExecutionResult(snapshot, warnings);
		}

		private static void EmbedAttribute(string fileType, string attributeName, string value, ref string content)
		{
			var rx = GetRegex(fileType, attributeName);

			var match = rx.Match(content);
			if (!match.Success)
				throw new InvalidConfigurationException(
					String.Format(
						"AssemblyInfo: attribute [{0}] not found. Hint: Embed() substitutes values into existing attributes only," + 
						" so you should add [assembly: {0}(\"\")] line into your AssemblyInfo file.", attributeName));			

			if (match.Groups.Count != 2)
				throw new InvalidOperationException("Inconsistency detected: attribute search regex should match exactly one group.");

			var group = match.Groups[1];
			content = content.Substring(0, group.Index) + Escape(value) + content.Substring(group.Index + group.Length);
		}

		private static Regex GetRegex(string fileType, string attributeName)
		{
			var key = attributeName + fileType;

			Regex rx;
			if (!RegExps.TryGetValue(key, out rx))
			{
				string pattern;
				if (!Patterns.TryGetValue(fileType, out pattern))
					throw new InvalidConfigurationException(String.Format("AssemblyInfo: '{0}' file type not supported.", fileType));

				rx = new Regex(String.Format(pattern, attributeName), RegexOptions.CultureInvariant | RegexOptions.Multiline);
				RegExps.Add(key, rx);
			}
			
			return rx;
		}

		private static string Escape(string value)
		{
			var sb = new StringBuilder();
			sb.Append('"');

			var start = 0;
			do
			{
				var index = value.IndexOfAny(new[] {'"', '\r', '\n', '\t', '\\'}, start);
				if (index < 0)
					break;
				
				sb.Append(value.Substring(start, index - start));
				sb.Append('\\');

				switch (value[index])
				{
					case '\r':
						sb.Append('r');
						break;
					case '\n':
						sb.Append('n');
						break;
					case '\t':
						sb.Append('t');
						break;
					default:
						sb.Append(value[index]);
						break;
				}

				start = index + 1;				
			} while (start < value.Length);

			if (start < value.Length)
			{
				sb.Append(value.Substring(start, value.Length - start));
			}
			sb.Append('"');

			return sb.ToString();
		}
	}
}