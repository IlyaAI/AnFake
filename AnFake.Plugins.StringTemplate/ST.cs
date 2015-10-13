using System;
using System.Linq;
using System.Net;
using AnFake.Core;
using AnFake.Core.Exceptions;
using Antlr4.StringTemplate;

namespace AnFake.Plugins.StringTemplate
{
	// ReSharper disable once InconsistentNaming
	public static class ST
	{
		private class ObjectModelAdaptor : Antlr4.StringTemplate.Misc.ObjectModelAdaptor
		{
			private readonly Func<string, string> Escaper;

			public ObjectModelAdaptor(Func<string, string> escaper)
			{
				Escaper = escaper;
			}

			public override object GetProperty(Interpreter interpreter, TemplateFrame frame, object o, object property, string propertyName)
			{
				var ret = base.GetProperty(interpreter, frame, o, property, propertyName);

				var s = ret as String;
				return s != null ? Escaper(s) : ret;
			}
		}

		public sealed class Params
		{
			public Func<string, string> Escaper;

			internal Params()
			{
				Escaper = Html;
			}

			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		public static readonly Func<string, string> Html = 
			s => WebUtility.HtmlEncode(s);

		public static Params Defaults { get; private set; }

		static ST()
		{
			Defaults = new Params();
		}

		public static void PlugIn()
		{
			Plugin.Register<STPlugin>()
				.AsSelf();
		}

		public static string Render(string template, object context)
		{
			return Render(template, context, p => { });
		}

		public static string Render(string template, object context, Action<Params> setParams)
		{
			var parameters = Defaults.Clone();
			setParams(parameters);

			var stg = new TemplateGroup('$', '$');
			stg.DefineTemplate("main", template, new[] { "_" });

			var tmpl = BuildUpTemplate(stg, context, parameters);
			return tmpl.Render();
		}

		public static void Render(FileItem tmplFile, FileItem targetFile, object context)
		{
			Render(tmplFile, targetFile, context, p => { });
		}

		public static void Render(FileItem tmplFile, FileItem targetFile, object context, Action<Params> setParams)
		{
			var parameters = Defaults.Clone();
			setParams(parameters);

			var tmpl = BuildUpTemplate(new TemplateGroupFile(tmplFile.Path.Full), context, parameters);
			Text.WriteTo(targetFile, tmpl.Render());
		}

		private static Template BuildUpTemplate(TemplateGroup stg, object context, Params parameters)
		{			
			stg.RegisterModelAdaptor(typeof (Object), new ObjectModelAdaptor(parameters.Escaper));

			var tmpl = stg.GetInstanceOf("main");
			if (tmpl == null)
				throw new InvalidConfigurationException("Template 'main' not found.");

			var args = tmpl.GetAttributes();
			if (args.Count != 1)
				throw new InvalidConfigurationException("Template 'main' is expected to have single argument.");

			tmpl.Add(args.Keys.First(), context);

			return tmpl;
		}
	}
}