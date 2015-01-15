using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AnFake.Api;

namespace AnFake.Core
{
	/// <summary>
	///		Represents NuGet package manager tool.
	/// </summary>
	/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
	public static class NuGet
	{
		private static readonly string[] Locations =
		{
			".nuget/NuGet.exe"
		};

		/// <summary>
		///		NuGet parameters.
		/// </summary>
		public sealed class Params
		{
			/// <summary>
			///		Version of package to be installed.
			/// </summary>
			public string Version;

			/// <summary>
			///		Output folder for installed package. Default: 'packages'
			/// </summary>
			public FileSystemPath OutputDirectory;

			/// <summary>
			///		Whether to include referenced projects into package or not.
			/// </summary>
			public bool IncludeReferencedProjects;

			/// <summary>
			///		Do not perform package analysis (i.e. disables warnings).
			/// </summary>
			public bool NoPackageAnalysis;

			/// <summary>
			///		Do not exclude folders started from dot.
			/// </summary>			
			public bool NoDefaultExcludes;

			/// <summary>
			///		Access key for package push.
			/// </summary>
			public string AccessKey;

			/// <summary>
			///		Package source URL.
			/// </summary>
			public string SourceUrl;

			/// <summary>
			///		Timeout for NuGet operation. Default: TimeSpan.MaxValue
			/// </summary>
			public TimeSpan Timeout;

			/// <summary>
			///		Path to 'nuget.exe'. Default: '.nuget/NuGet.exe'
			/// </summary>
			public FileSystemPath ToolPath;

			/// <summary>
			///		Additional nuget arguments passed as is.
			/// </summary>
			public string ToolArguments;

			internal Params()
			{
				OutputDirectory = "packages".AsPath();
				Timeout = TimeSpan.MaxValue;				
			}

			/// <summary>
			///		Clones parameters.
			/// </summary>
			/// <returns>copy of original parameters</returns>
			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		/// <summary>
		///		Default NuGet parameters.
		/// </summary>
		public static Params Defaults { get; private set; }

		static NuGet()
		{
			Defaults = new Params();

			MyBuild.Initialized += (s, p) =>
			{
				Defaults.ToolPath = Locations.AsFileSet().Select(x => x.Path).FirstOrDefault();
			};
		}

		/// <summary>
		///		Equals to 'nuget.exe install'.
		/// </summary>		
		/// <param name="packageId">id of package to be installed</param>
		/// <param name="setParams">action which overrides default parameters</param>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		/// <example>
		/// <code>
		///		NuGet.Install(
        ///			"NUnitTestAdapter",
        ///			(fun p -> 
        ///				p.Version &lt;- "1.2"))
		/// </code>
		/// </example>
		public static void Install(string packageId, Action<Params> setParams)
		{
			if (String.IsNullOrEmpty(packageId))
				throw new ArgumentException("NuGet.Install(packageId[, setParams]): packageId must not be null or empty");			
			if (setParams == null)
				throw new ArgumentException("NuGet.Install(packageId, setParams): setParams must not be null");
			
			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureToolPath(parameters);
			// TODO: check other parameters

			Trace.InfoFormat("NuGet.Install => {0}", packageId);

			var args = new Args("-", " ")
				.Command("install")
				.Param(packageId)
				.Option("Version", parameters.Version)
				.Option("OutputDirectory", parameters.OutputDirectory)
				.Option("NonInteractive", true)
				.Other(parameters.ToolArguments);			

			var result = Process.Run(p =>
			{
				p.FileName = parameters.ToolPath;
				p.Timeout = parameters.Timeout;
				p.Arguments = args.ToString();
			});

			result
				.FailIfAnyError("Target terminated due to NuGet errors.")
				.FailIfExitCodeNonZero(String.Format("NuGet.Install failed with exit code {0}. Package: {1}", result.ExitCode, packageId));
		}

		/// <summary>
		///		Creates package spec of version 2.0
		/// </summary>
		/// <param name="setMeta">action which sets package metadata</param>
		/// <returns>package spec instance</returns>
		public static NuSpec.v20.Package Spec20(Action<NuSpec.v20.Metadata> setMeta)
		{
			var pkg = new NuSpec.v20.Package { Metadata = new NuSpec.v20.Metadata() };

			setMeta(pkg.Metadata);

			return pkg;
		}

		/// <summary>
		///		Creates package spec of version 2.5
		/// </summary>
		/// <param name="setMeta">action which sets package metadata</param>
		/// <returns>package spec instance</returns>
		public static NuSpec.v25.Package Spec25(Action<NuSpec.v25.Metadata> setMeta)
		{
			var pkg = new NuSpec.v25.Package { Metadata = new NuSpec.v25.Metadata() };

			setMeta(pkg.Metadata);

			return pkg;			
		}

		/// <summary>
		///		Equals to 'nuget.exe pack'.
		/// </summary>
		/// <remarks>
		///		Files to be packed must be specified via <c>AddFiles</c> on package spec.
		///		Package will be created in given destination folder.
		/// </remarks>
		/// <param name="nuspec">package spec returned by <see cref="Spec20"/> or <see cref="Spec25"/></param>
		/// <param name="dstFolder">output folder for created package</param>
		/// <returns>file item representing created package</returns>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		public static FileItem Pack(NuSpec.IPackage nuspec, FileSystemPath dstFolder)
		{
			return Pack(nuspec, dstFolder, dstFolder, p => { });
		}

		/// <summary>
		///		Equals to 'nuget.exe pack'.
		/// </summary>
		/// <remarks>
		///		Files to be packed must be specified via <c>AddFiles</c> on package spec.
		///		Package will be created in given destination folder.
		/// </remarks>
		/// <param name="nuspec">package spec returned by <see cref="Spec20"/> or <see cref="Spec25"/></param>
		/// <param name="dstFolder">output folder for created package</param>
		/// <param name="setParams">action which overrides default parameters</param>
		/// <returns>file item representing created package</returns>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		public static FileItem Pack(NuSpec.IPackage nuspec, FileSystemPath dstFolder, Action<Params> setParams)
		{
			return Pack(nuspec, dstFolder, dstFolder, setParams);
		}

		/// <summary>
		///		Equals to 'nuget.exe pack'.
		/// </summary>
		/// <remarks>
		///		Files to be packed will be taken from specified source folder.
		///		Package will be created in given destination folder.
		/// </remarks>
		/// <param name="nuspec">package spec returned by <see cref="Spec20"/> or <see cref="Spec25"/></param>
		/// <param name="srcFolder">source folder containing files to be packed</param>
		/// <param name="dstFolder">output folder for created package</param>
		/// <returns>file item representing created package</returns>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		public static FileItem Pack(NuSpec.IPackage nuspec, FileSystemPath srcFolder, FileSystemPath dstFolder)
		{
			return Pack(nuspec, srcFolder, dstFolder, p => { });
		}

		/// <summary>
		///		Equals to 'nuget.exe pack'.
		/// </summary>
		/// <remarks>
		///		Files to be packed will be taken from specified source folder.
		///		Package will be created in given destination folder.
		/// </remarks>
		/// <param name="nuspec">package spec returned by <see cref="Spec20"/> or <see cref="Spec25"/></param>
		/// <param name="srcFolder">source folder containing files to be packed</param>
		/// <param name="dstFolder">output folder for created package</param>
		/// <param name="setParams">action which overrides default parameters</param>
		/// <returns>file item representing created package</returns>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		/// <example>
		/// <code>
		///		let nugetFiles = 
		///			~~".out" % "*.dll"
		///			+ "*.exe"
		///			+ "*.config"
		/// 
		///		let nuspec = NuGet.Spec25(fun meta -> 
        ///			meta.Id &lt;- "AnFake"
        ///			meta.Version &lt;- version
        ///			meta.Authors &lt;- "Ilya A. Ivanov"
        ///			meta.Description &lt;- "AnFake: Another F# Make"
		///		)
		///
		///		nuspec.AddFiles(nugetFiles, "")
		///
		///		NuGet.Pack(nuspec, ~~".out", fun p -> 
        ///			p.NoPackageAnalysis &lt;- true
        ///			p.NoDefaultExcludes &lt;- true)
        ///		|> ignore
		/// </code>
		/// </example>
		public static FileItem Pack(NuSpec.IPackage nuspec, FileSystemPath srcFolder, FileSystemPath dstFolder, Action<Params> setParams)
		{
			if (nuspec == null)
				throw new ArgumentException("NuGet.Pack(nuspec, srcFolder, dstFolder, setParams): nuspec must not be null");
			if (srcFolder == null)
				throw new ArgumentException("NuGet.Pack(nuspec, srcFolder, dstFolder, setParams): srcFolder must not be null");
			if (dstFolder == null)
				throw new ArgumentException("NuGet.Pack(nuspec, srcFolder, dstFolder, setParams): dstFolder must not be null");
			if (setParams == null)
				throw new ArgumentException("NuGet.Pack(nuspec, srcFolder, dstFolder, setParams): setParams must not be null");

			nuspec.Validate();			

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureToolPath(parameters);
			// TODO: check other parameters			

			var nuspecFile = GenerateNuspecFile(nuspec, srcFolder);

			Trace.InfoFormat("NuGet.Pack => {0}", nuspecFile);

			var args = new Args("-", " ")
				.Command("pack")
				.Param(nuspecFile.Path.Full)
				.Option("OutputDirectory", dstFolder)
				.Option("NoPackageAnalysis", parameters.NoPackageAnalysis)
				.Option("NoDefaultExcludes", parameters.NoDefaultExcludes)
				.Option("IncludeReferencedProjects", parameters.IncludeReferencedProjects)
				.Other(parameters.ToolArguments);

			Folders.Create(dstFolder);

			var result = Process.Run(p =>
			{
				p.FileName = parameters.ToolPath;
				p.Timeout = parameters.Timeout;
				p.Arguments = args.ToString();				
			});

			result
				.FailIfAnyError("Target terminated due to NuGet errors.")
				.FailIfExitCodeNonZero(
					String.Format("NuGet.Pack failed with exit code {0}. Package: {1}", result.ExitCode, nuspecFile));

			var pkgPath = dstFolder / String.Format("{0}.{1}.nupkg", nuspec.Id, nuspec.Version);

			return pkgPath.AsFile();
		}		

		/// <summary>
		///		Equals to 'nuget.exe push'.
		/// </summary>
		/// <param name="package">path to package to be pushed</param>
		/// <param name="setParams">action which overrides default parameters</param>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		/// <example>
		/// <code>
		///		NuGet.Push(
        ///			~~".out" / "AnFake.0.9.nupkg",
        ///			fun p -> 
        ///				p.AccessKey &lt;- "YOUR ACCESS KEY"
        ///				p.SourceUrl &lt;- "SOURCE URL HERE")		
		/// </code>
		/// </example>
		public static void Push(FileItem package, Action<Params> setParams)
		{
			if (package == null)
				throw new ArgumentException("NuGet.Push(package, setParams): package must not be null");
			if (setParams == null)
				throw new ArgumentException("NuGet.Push(package, setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureToolPath(parameters);

			if (String.IsNullOrEmpty(parameters.AccessKey))
				throw new ArgumentException("NuGet.Params.AccessKey must not be null or empty");

			// TODO: check other parameters

			Trace.InfoFormat("NuGet.Push => {0}", package);

			var args = new Args("-", " ")
				.Command("push")
				.Param(package.Path.Full)
				.Param(parameters.AccessKey)
				.Option("s", parameters.SourceUrl)
				.Other(parameters.ToolArguments);

			var result = Process.Run(p =>
			{
				p.FileName = parameters.ToolPath;
				p.Timeout = parameters.Timeout;
				p.Arguments = args.ToString();				
			});

			result
				.FailIfAnyError("Target terminated due to NuGet errors.")
				.FailIfExitCodeNonZero(String.Format("NuGet.Push failed with exit code {0}. Package: {1}", result.ExitCode, package));

			Trace.SummaryFormat("NuGet.Push: {0} @ {1}", package.Name, parameters.SourceUrl);
		}

		private static void EnsureToolPath(Params parameters)
		{
			if (parameters.ToolPath == null)
				throw new ArgumentException(
					String.Format(
						"NuGet.Params.ToolPath must not be null.\nHint: probably, NuGet.exe not found.\nSearch path:\n  {0}",
						String.Join("\n  ", Locations)));
		}

		private static FileItem GenerateNuspecFile(NuSpec.IPackage nuspec, FileSystemPath srcFolder)
		{
			var nuspecFile = (srcFolder / nuspec.Id + ".nuspec").AsFile();
			
			Folders.Create(nuspecFile.Folder);
			using (var stm = new FileStream(nuspecFile.Path.Full, FileMode.Create, FileAccess.Write))
			{
				new XmlSerializer(nuspec.GetType()).Serialize(stm, nuspec);
			}

			return nuspecFile;
		}
	}
}